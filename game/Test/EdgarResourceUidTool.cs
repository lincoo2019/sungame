using Godot;

public partial class EdgarResourceUidTool : Node
{
	private static readonly string[] ResourcePaths = new string[6] { "res://Asset/Anime/Character/Zombie/Boss/EdgarII/ZombieBossEdgarII.tscn", "res://Asset/Anime/Character/Zombie/Boss/EdgarII/Effect/EdgarSPDown.tscn", "res://Asset/Anime/Character/Zombie/Boss/EdgarII/Config/TowerDefenseZombieBossEdgarII.tres", "res://Asset/Anime/Character/Zombie/Boss/EdgarII/Packet/ZombieBossEdgarII.tres", "res://Asset/Anime/Character/Zombie/Boss/EdgarII/Scene/TowerDefenseZombieBossEdgarII.tscn", "res://Asset/Config/Projectile/EdgarII/ZombieBossEdgarIIFireball.tres" };

	public override async void _Ready()
	{
		ProjectResourceUidRepairResult characterRepair = await ProjectResourceUidRepairTool.RepairProjectAsync("res://Asset/Anime/Character/Zombie/Boss/EdgarII");
		ProjectResourceUidRepairResult projectResourceUidRepairResult = await ProjectResourceUidRepairTool.RepairProjectAsync("res://Asset/Config/Projectile/EdgarII");
		ProjectResourceUidRegistrationResult projectResourceUidRegistrationResult = ProjectResourceUidRepairTool.RegisterUidMappings(characterRepair.UidMappings);
		ProjectResourceUidRegistrationResult projectResourceUidRegistrationResult2 = ProjectResourceUidRepairTool.RegisterUidMappings(projectResourceUidRepairResult.UidMappings);
		bool flag = characterRepair.Errors == 0 && projectResourceUidRepairResult.Errors == 0 && projectResourceUidRegistrationResult.Errors == 0 && projectResourceUidRegistrationResult2.Errors == 0;
		string[] resourcePaths = ResourcePaths;
		foreach (string text in resourcePaths)
		{
			long resourceUid = ResourceLoader.GetResourceUid(text);
			string text2 = ((resourceUid == -1) ? "invalid" : ResourceUid.IdToText(resourceUid));
			if (resourceUid == -1)
			{
				flag = false;
			}
			GD.Print("EDGAR_UID path=" + text + " uid=" + text2);
		}
		GD.Print($"EDGAR_UID_RESULT passed={flag}");
		GetTree().Quit((!flag) ? 1 : 0);
	}
}
