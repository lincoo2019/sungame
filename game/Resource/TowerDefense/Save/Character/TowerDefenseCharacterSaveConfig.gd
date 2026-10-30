
class_name TowerDefenseCharacterSaveConfig extends Resource

@export var nodeName: StringName
@export var packetName: String
@export var pos: Vector2
@export var gridPos: Vector2i
@export var height: float
@export var economyOwnerAccountId: String = ""

@export var characterNodeSave: TowerDefenseNodeSaveConfig

@export var instanceSave: Dictionary = {}
@export var buffSave: Array[Dictionary] = []
@export var currentArmor: Array[String] = []
@export var currentCustom: Array[String] = []
@export var timeScale: float = 1.0
@export var timeScaleInit: float = 1.0
@export var timeScaleSave: float = 1.0

@export var stateMachineSave: Dictionary = {}
@export var stateChartSave: Dictionary = {}
@export var spriteSave: Dictionary = {}
@export var componentSaveList: Array[Dictionary] = []
@export var characterFlags: Dictionary = {}
@export var zombieExtraSave: Dictionary = {}
@export var scaleX: float = 1.0
@export var scaleY: float = 1.0
@export var transformPointScaleX: float = 1.0
@export var transformPointScaleY: float = 1.0
@export var overrideSave: Dictionary = {}
@export var variantSave: Dictionary = {}
@export var canChangeCostSave: bool = true
@export var changeCostListSave: Array[Dictionary] = []

@export var z: float = 0.0
@export var ySpeed: float = 0.0
@export var isGround: bool = true
@export var cellPercentage: float = 0.5
@export var cost: float = 0.0
