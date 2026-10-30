//
//  MoonlightCrypto.swift
//  QuickRecorder
//
//  Moonlight 配对 / 控制流所需的密码学原语：
//  - SHA-256、AES-128-GCM、AES-128-ECB
//  - 自签名证书与私钥的生成与加载（借助系统 openssl 生成，Security.framework 使用）
//  - X.509 原始签名字节的 DER 提取（配对哈希需要）
//  - RSA-SHA256 签名 / 验签（PKCS#1 v1.5）
//

import Foundation
import CryptoKit
import CommonCrypto
import Security

enum MLCryptoError: Error, LocalizedError {
    case cryptoFailure(String)
    case certificateFailure(String)

    var errorDescription: String? {
        switch self {
        case .cryptoFailure(let m): return "加密失败：\(m)"
        case .certificateFailure(let m): return "证书失败：\(m)"
        }
    }
}

// MARK: - 哈希

enum MLHash {
    static func sha256(_ data: [UInt8]) -> [UInt8] {
        Array(SHA256.hash(data: Data(data)))
    }

    static func sha256(_ data: Data) -> [UInt8] {
        Array(SHA256.hash(data: data))
    }
}

// MARK: - AES-GCM（控制流加密，NIST SP800-38D 确定性 IV）

enum MLAESGCM {
    /// 加密，返回 [tag(16)][ciphertext]（与 Sunshine 的 GCM 输出布局一致）
    static func seal(key: [UInt8], iv: [UInt8], plaintext: [UInt8]) throws -> [UInt8] {
        guard key.count == 16 else { throw MLCryptoError.cryptoFailure("AES-GCM 密钥必须为 16 字节") }
        let symKey = SymmetricKey(data: Data(key))
        let nonce = try AES.GCM.Nonce(data: Data(iv))
        let sealed = try AES.GCM.seal(Data(plaintext), using: symKey, nonce: nonce)
        return Array(sealed.tag) + Array(sealed.ciphertext)
    }

    /// 解密，输入为 [tag(16)][ciphertext]
    static func open(key: [UInt8], iv: [UInt8], taggedCipher: [UInt8]) throws -> [UInt8] {
        guard key.count == 16 else { throw MLCryptoError.cryptoFailure("AES-GCM 密钥必须为 16 字节") }
        guard taggedCipher.count >= 16 else { throw MLCryptoError.cryptoFailure("AES-GCM 密文过短") }
        let symKey = SymmetricKey(data: Data(key))
        let nonce = try AES.GCM.Nonce(data: Data(iv))
        let tag = Data(taggedCipher[0..<16])
        let cipher = Data(taggedCipher[16...])
        let box = try AES.GCM.SealedBox(nonce: nonce, ciphertext: cipher, tag: tag)
        return Array(try AES.GCM.open(box, using: symKey))
    }
}

// MARK: - AES-ECB（配对流程使用，无填充）

enum MLAESECB {
    static func encrypt(key: [UInt8], plaintext: [UInt8]) throws -> [UInt8] {
        try crypt(key: key, data: plaintext, encrypt: true)
    }

    static func decrypt(key: [UInt8], ciphertext: [UInt8]) throws -> [UInt8] {
        try crypt(key: key, data: ciphertext, encrypt: false)
    }

    private static func crypt(key: [UInt8], data: [UInt8], encrypt: Bool) throws -> [UInt8] {
        guard key.count == 16 else { throw MLCryptoError.cryptoFailure("AES-ECB 密钥必须为 16 字节") }
        guard data.count % 16 == 0 else { throw MLCryptoError.cryptoFailure("AES-ECB 数据必须为 16 字节整数倍") }
        let keyLength = key.count
        let dataLength = data.count
        var out = [UInt8](repeating: 0, count: dataLength + kCCBlockSizeAES128)
        var moved = 0
        let status = key.withUnsafeBytes { keyPtr -> CCCryptorStatus in
            data.withUnsafeBytes { dataPtr in
                out.withUnsafeMutableBytes { outPtr in
                    // 注意：闭包内不能读取 out（会造成独占访问冲突），改用 outPtr.count
                    CCCrypt(CCOperation(encrypt ? kCCEncrypt : kCCDecrypt),
                            CCAlgorithm(kCCAlgorithmAES),
                            CCOptions(kCCOptionECBMode),
                            keyPtr.baseAddress, keyLength,
                            nil,
                            dataPtr.baseAddress, dataLength,
                            outPtr.baseAddress, outPtr.count,
                            &moved)
                }
            }
        }
        guard status == kCCSuccess else { throw MLCryptoError.cryptoFailure("AES-ECB 状态码 \(status)") }
        return Array(out[0..<moved])
    }
}

// MARK: - AES-CBC（音频流加密）

/// 音频通道加密：客户端 AudioStream.c 的 decodeInputData 用 AES-128-CBC 解密每个音频负载
/// （密钥 = /launch 的 rikey，IV = BE32(riKeyId + 序列号) 后跟 12 个 0，CIPHER_FLAG_FINISH
/// 会去掉 PKCS#7 填充）。若客户端请求了音频加密而我们发明文，客户端会把明文当密文解密，
/// 得到乱码 → opus 解码失败 → 完全静音。这里做与客户端对称的加密。
enum MLAESCBC {
    static func encrypt(key: [UInt8], iv: [UInt8], plaintext: [UInt8]) throws -> [UInt8] {
        try crypt(key: key, iv: iv, data: plaintext, encrypt: true)
    }

    static func decrypt(key: [UInt8], iv: [UInt8], ciphertext: [UInt8]) throws -> [UInt8] {
        try crypt(key: key, iv: iv, data: ciphertext, encrypt: false)
    }

    private static func crypt(key: [UInt8], iv: [UInt8], data: [UInt8], encrypt: Bool) throws -> [UInt8] {
        guard key.count == 16 else { throw MLCryptoError.cryptoFailure("AES-CBC 密钥必须为 16 字节") }
        guard iv.count == 16 else { throw MLCryptoError.cryptoFailure("AES-CBC IV 必须为 16 字节") }
        var out = [UInt8](repeating: 0, count: data.count + kCCBlockSizeAES128)
        var moved = 0
        let status = key.withUnsafeBytes { keyPtr -> CCCryptorStatus in
            iv.withUnsafeBytes { ivPtr in
                data.withUnsafeBytes { dataPtr in
                    out.withUnsafeMutableBytes { outPtr in
                        // 注意：闭包内不能读取 out（会造成独占访问冲突），改用 outPtr.count
                        CCCrypt(CCOperation(encrypt ? kCCEncrypt : kCCDecrypt),
                                CCAlgorithm(kCCAlgorithmAES),
                                CCOptions(kCCOptionPKCS7Padding),
                                keyPtr.baseAddress, key.count,
                                ivPtr.baseAddress,
                                dataPtr.baseAddress, data.count,
                                outPtr.baseAddress, outPtr.count,
                                &moved)
                    }
                }
            }
        }
        guard status == kCCSuccess else { throw MLCryptoError.cryptoFailure("AES-CBC 状态码 \(status)") }
        return Array(out[0..<moved])
    }
}

// MARK: - 最小 DER 读取器

/// 用于从 X.509 证书中取出原始签名 BIT STRING（配对哈希需要证书签名，而非证书本身的哈希）
struct MLDERReader {
    let bytes: [UInt8]
    private(set) var offset = 0

    init(_ bytes: [UInt8]) { self.bytes = bytes }

    var isAtEnd: Bool { offset >= bytes.count }

    mutating func readTLV() throws -> (tag: UInt8, content: [UInt8]) {
        guard offset + 2 <= bytes.count else { throw MLCryptoError.certificateFailure("DER 数据截断") }
        let tag = bytes[offset]
        offset += 1
        var length = Int(bytes[offset])
        offset += 1
        if length & 0x80 != 0 {
            let count = length & 0x7F
            guard count > 0, count <= 4, offset + count <= bytes.count else {
                throw MLCryptoError.certificateFailure("DER 长度非法")
            }
            length = 0
            for _ in 0..<count {
                length = (length << 8) | Int(bytes[offset])
                offset += 1
            }
        }
        guard offset + length <= bytes.count else { throw MLCryptoError.certificateFailure("DER 内容越界") }
        let content = Array(bytes[offset..<(offset + length)])
        offset += length
        return (tag, content)
    }

    mutating func skipTLV() throws {
        _ = try readTLV()
    }
}

enum MLX509 {
    /// 提取证书的原始签名字节（X509_get0_signature 等价语义）
    static func rawSignature(der: [UInt8]) throws -> [UInt8] {
        var reader = MLDERReader(der)
        let (outerTag, outerContent) = try reader.readTLV()
        guard outerTag == 0x30 else { throw MLCryptoError.certificateFailure("证书不是 SEQUENCE") }
        var inner = MLDERReader(outerContent)
        try inner.skipTLV() // tbsCertificate
        try inner.skipTLV() // signatureAlgorithm
        let (sigTag, sigContent) = try inner.readTLV()
        guard sigTag == 0x03 else { throw MLCryptoError.certificateFailure("签名不是 BIT STRING") }
        guard sigContent.count >= 1 else { throw MLCryptoError.certificateFailure("签名为空") }
        // BIT STRING 首字节为未使用位计数，需要去掉
        return Array(sigContent[1...])
    }

    /// PEM → DER
    static func der(fromPEM pem: String) -> [UInt8]? {
        let base64 = pem
            .components(separatedBy: "\n")
            .filter { !$0.hasPrefix("-----") && !$0.trimmingCharacters(in: .whitespaces).isEmpty }
            .joined()
        return Data(base64Encoded: base64).map { Array($0) }
    }

    /// DER → PEM
    static func pem(fromDER der: [UInt8], label: String) -> String {
        let base64 = Data(der).base64EncodedString()
        var lines: [String] = []
        var idx = base64.startIndex
        while idx < base64.endIndex {
            let end = base64.index(idx, offsetBy: 64, limitedBy: base64.endIndex) ?? base64.endIndex
            lines.append(String(base64[idx..<end]))
            idx = end
        }
        return "-----BEGIN \(label)-----\n" + lines.joined(separator: "\n") + "\n-----END \(label)-----\n"
    }

    /// 规范化 PEM（仅保留有效行），用于与已配对客户端证书做精确比对
    static func canonicalPEM(_ pem: String) -> String? {
        guard let der = der(fromPEM: pem) else { return nil }
        return self.pem(fromDER: der, label: "CERTIFICATE")
    }
}

// MARK: - 证书与密钥管理

/// 持有服务端自签名证书与私钥，并提供配对所需的签名/验签能力
final class MLIdentity {
    static let p12Passphrase = "quickrecorder-streaming"

    let certificatePEM: String
    let certificateDER: [UInt8]
    /// 证书的原始签名字节
    let certificateSignature: [UInt8]
    private let privateKey: SecKey
    /// TLS 身份（HTTPS 服务端使用）
    let secIdentity: SecIdentity
    /// 专用钥匙串，必须保持强引用（SecIdentity 依赖它）
    private let keychain: SecKeychain

    /// 加载已有证书；不存在时用系统 openssl 生成自签名证书
    init(directory: URL) throws {
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)

        let certURL = directory.appendingPathComponent("cacert.pem")
        let keyURL = directory.appendingPathComponent("cakey.pem")
        let p12URL = directory.appendingPathComponent("identity.p12")

        if !MLIdentity.assetsComplete(certURL: certURL, keyURL: keyURL, p12URL: p12URL) {
            try MLIdentity.generateSelfSigned(certURL: certURL, keyURL: keyURL, p12URL: p12URL)
        }

        // 取得身份（必要时重新生成并重试一次）
        var material: (identity: SecIdentity, key: SecKey, keychain: SecKeychain)
        do {
            material = try MLIdentity.loadIdentity(directory: directory, p12URL: p12URL)
        } catch {
            try MLIdentity.generateSelfSigned(certURL: certURL, keyURL: keyURL, p12URL: p12URL)
            material = try MLIdentity.loadIdentity(directory: directory, p12URL: p12URL)
        }

        guard let certPEM = try? String(contentsOf: certURL, encoding: .utf8) else {
            throw MLCryptoError.certificateFailure("无法读取证书 \(certURL.path)")
        }
        guard let certDER = MLX509.der(fromPEM: certPEM) else {
            throw MLCryptoError.certificateFailure("证书 PEM 解析失败")
        }

        self.certificatePEM = MLX509.canonicalPEM(certPEM) ?? certPEM
        self.certificateDER = certDER
        self.certificateSignature = try MLX509.rawSignature(der: certDER)
        self.privateKey = material.key
        self.secIdentity = material.identity
        self.keychain = material.keychain
    }

    private static func assetsComplete(certURL: URL, keyURL: URL, p12URL: URL) -> Bool {
        let fileManager = FileManager.default
        return fileManager.fileExists(atPath: certURL.path)
            && fileManager.fileExists(atPath: keyURL.path)
            && fileManager.fileExists(atPath: p12URL.path)
    }

    // MARK: 钥匙串与身份

    private static let keychainFileName = "identity.keychain-db"
    private static let keychainPasswordFile = "identity.keychain.pass"
    private static let securityTool = "/usr/bin/security"

    /// 建立专用钥匙串并导入 PKCS#12，返回可直接使用的身份与私钥。
    ///
    /// 说明：`SecPKCS12Import` 默认会写入登录钥匙串，且导入到默认/新建钥匙串时
    /// 会因 ACL 触发"输入钥匙串密码"授权框（用户无法回答随机口令）。
    /// 因此这里改用 `security` 命令行以 `-A`（允许所有应用访问）导入，彻底避免授权框；
    /// 进程内只做打开、解锁与取身份这类不会触发授权的操作。
    private static func loadIdentity(directory: URL,
                                     p12URL: URL) throws -> (SecIdentity, SecKey, SecKeychain) {
        let keychainURL = directory.appendingPathComponent(keychainFileName)
        let passwordURL = directory.appendingPathComponent(keychainPasswordFile)
        let password = loadOrCreateKeychainPassword(at: passwordURL)

        // 已存在的钥匙串先删除，保证导入是干净的（避免重复项导致导入失败）
        try? FileManager.default.removeItem(at: keychainURL)

        _ = runSecurity(["create-keychain", "-p", password, keychainURL.path])
        _ = runSecurity(["unlock-keychain", "-p", password, keychainURL.path])
        // 关闭自动锁定，避免运行中钥匙串被锁
        _ = runSecurity(["set-keychain-settings", "-lut", "86400", keychainURL.path])

        let importResult = runSecurity([
            "import", p12URL.path, "-k", keychainURL.path,
            "-P", p12Passphrase, "-A",
        ])
        guard importResult.status == 0 else {
            throw MLCryptoError.certificateFailure("钥匙串导入失败：\(importResult.output)")
        }

        var keychainRef: SecKeychain?
        guard SecKeychainOpen(keychainURL.path, &keychainRef) == errSecSuccess, let keychain = keychainRef else {
            throw MLCryptoError.certificateFailure("无法打开钥匙串 \(keychainURL.lastPathComponent)")
        }
        _ = SecKeychainUnlock(keychain, UInt32(password.utf8.count), password, true)

        // 从证书文件解析出 SecCertificate，再取匹配的身份
        guard let certPEM = try? String(contentsOf: p12URL.deletingLastPathComponent()
            .appendingPathComponent("cacert.pem"), encoding: .utf8),
            let certDER = MLX509.der(fromPEM: certPEM),
            let certificate = SecCertificateCreateWithData(nil, Data(certDER) as CFData) else {
            throw MLCryptoError.certificateFailure("证书解析失败，无法建立身份")
        }

        var identity: SecIdentity?
        let identityStatus = SecIdentityCreateWithCertificate(keychain, certificate, &identity)
        guard identityStatus == errSecSuccess, let identity else {
            throw MLCryptoError.certificateFailure("无法建立身份（状态 \(identityStatus)）")
        }

        var key: SecKey?
        guard SecIdentityCopyPrivateKey(identity, &key) == errSecSuccess, let privateKey = key else {
            throw MLCryptoError.certificateFailure("无法取得私钥")
        }
        return (identity, privateKey, keychain)
    }

    private static func loadOrCreateKeychainPassword(at url: URL) -> String {
        if let stored = try? String(contentsOf: url, encoding: .utf8) {
            let trimmed = stored.trimmingCharacters(in: .whitespacesAndNewlines)
            if !trimmed.isEmpty { return trimmed }
        }
        let generated = MLRandom.bytes(16).mlHexString()
        try? generated.write(to: url, atomically: true, encoding: .utf8)
        try? FileManager.default.setAttributes([.posixPermissions: 0o600], ofItemAtPath: url.path)
        return generated
    }

    @discardableResult
    private static func runSecurity(_ arguments: [String]) -> (status: Int32, output: String) {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: securityTool)
        process.arguments = arguments
        let pipe = Pipe()
        process.standardError = pipe
        process.standardOutput = pipe
        do {
            try process.run()
        } catch {
            return (-1, error.localizedDescription)
        }
        process.waitUntilExit()
        let output = String(data: pipe.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8) ?? ""
        return (process.terminationStatus, output.trimmingCharacters(in: .whitespacesAndNewlines))
    }

    // MARK: 生成

    private static func generateSelfSigned(certURL: URL, keyURL: URL, p12URL: URL) throws {
        guard let openssl = MLIdentity.opensslPath else {
            throw MLCryptoError.certificateFailure("系统未找到 openssl，无法生成证书")
        }
        // 清理上次可能留下的不完整文件
        for url in [certURL, keyURL, p12URL] {
            try? FileManager.default.removeItem(at: url)
        }

        // 一条命令同时生成私钥与自签名证书。
        // 注意：macOS 自带的是 LibreSSL，其 genrsa 不支持 -traditional；这里不依赖私钥格式，
        // 后续统一通过 PKCS#12 取得身份与私钥。
        try run(openssl, [
            "req", "-x509", "-newkey", "rsa:2048", "-nodes",
            "-keyout", keyURL.path, "-out", certURL.path,
            "-days", "3650", "-subj", "/CN=sunGame Streaming Host",
        ])

        // 导出 PKCS#12。LibreSSL 默认用 RC2-40 加密证书，而 macOS 的 Security.framework
        // 不支持 RC2，必须显式指定 3DES/SHA1。
        try run(openssl, [
            "pkcs12", "-export", "-out", p12URL.path,
            "-inkey", keyURL.path, "-in", certURL.path,
            "-passout", "pass:\(MLIdentity.p12Passphrase)",
            "-certpbe", "PBE-SHA1-3DES",
            "-keypbe", "PBE-SHA1-3DES",
            "-macalg", "sha1",
        ])
    }

    private static func run(_ launchPath: String, _ args: [String]) throws {
        let p = Process()
        p.executableURL = URL(fileURLWithPath: launchPath)
        p.arguments = args
        let pipe = Pipe()
        p.standardError = pipe
        p.standardOutput = pipe
        try p.run()
        p.waitUntilExit()
        guard p.terminationStatus == 0 else {
            let msg = String(data: pipe.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8) ?? ""
            throw MLCryptoError.certificateFailure("openssl 执行失败（\(p.terminationStatus)）：\(msg)")
        }
    }

    private static var opensslPath: String? {
        for p in ["/usr/bin/openssl", "/opt/homebrew/bin/openssl", "/usr/local/bin/openssl"] {
            if FileManager.default.isExecutableFile(atPath: p) { return p }
        }
        return nil
    }

    /// RSA-SHA256 (PKCS#1 v1.5) 签名
    func sign(sha256 data: [UInt8]) throws -> [UInt8] {
        var error: Unmanaged<CFError>?
        guard let sig = SecKeyCreateSignature(privateKey, .rsaSignatureMessagePKCS1v15SHA256,
                                             Data(data) as CFData, &error) else {
            throw MLCryptoError.cryptoFailure(
                "签名失败：\(error?.takeRetainedValue().localizedDescription ?? "未知")")
        }
        return Array(sig as Data)
    }

    /// 用客户端证书公钥校验 RSA-SHA256 签名
    static func verify(certificatePEM: String, data: [UInt8], signature: [UInt8]) -> Bool {
        guard let der = MLX509.der(fromPEM: certificatePEM),
              let cert = SecCertificateCreateWithData(nil, Data(der) as CFData),
              let pub = SecCertificateCopyKey(cert) else { return false }
        var error: Unmanaged<CFError>?
        return SecKeyVerifySignature(pub, .rsaSignatureMessagePKCS1v15SHA256,
                                     Data(data) as CFData, Data(signature) as CFData, &error)
    }
}

// MARK: - 随机

enum MLRandom {
    static func bytes(_ count: Int) -> [UInt8] {
        var out = [UInt8](repeating: 0, count: count)
        _ = SecRandomCopyBytes(kSecRandomDefault, count, &out)
        return out
    }

    static func uint32() -> UInt32 {
        bytes(4).mlBE32(0)
    }
}
