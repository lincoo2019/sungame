using Godot;

[GlobalClass]
public partial class NpcTalkControl : Node2D
{
	public TowerDefenseBattleFeatureNpcTalk npcTalkFeature;

	private Control _npcRenderMount;

	public override void _Ready()
	{
		_npcRenderMount = GetNode<Control>("%NpcRenderMount");
	}

	public void AddNpc(NpcBase npc)
	{
		if (GodotObject.IsInstanceValid(npc))
		{
			if (!GodotObject.IsInstanceValid(_npcRenderMount))
			{
				_npcRenderMount = GetNodeOrNull<Control>("%NpcRenderMount");
			}
			if (GodotObject.IsInstanceValid(npc.sprite) && GodotObject.IsInstanceValid(_npcRenderMount))
			{
				npc.sprite.forceLocalRender = true;
				npc.sprite.SetRenderClipControl(_npcRenderMount);
			}
			AddChild(npc, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void ShowTalk(NpcBase npc, NpcTalkBaseConfig talk)
	{
		npc.Talk(talk.text, talk.anime, talk.audio);
	}

	public void ShowHand(NpcBase npc, NpcTalkHandConfig talk)
	{
		npc.Hand(talk);
	}
}
