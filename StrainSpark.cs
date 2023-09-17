using RWCustom;
using UnityEngine;

// Token: 0x020004B1 RID: 1201
public class StrainSpark : CosmeticSprite
{
	// Token: 0x06001EBA RID: 7866 RVA: 0x001C54C8 File Offset: 0x001C36C8
	public StrainSpark(Vector2 pos, Vector2 vel, float maxLifeTime, Color color)
	{
		this.lastPos = pos;
		this.pos = pos;
		this.vel = vel;
		this.color = color;
		this.dir = Custom.AimFromOneVectorToAnother(-vel, vel);
		this.lifeTime = Mathf.Lerp(5f, maxLifeTime, UnityEngine.Random.value);
		this.life = 1f;
		this.graphic = (UnityEngine.Random.value < 0.5f);
	}

	// Token: 0x06001EBB RID: 7867 RVA: 0x001C5540 File Offset: 0x001C3740
	public override void Update(bool eu)
	{
		base.Update(eu);
		this.life -= 1f / this.lifeTime;
		if (this.life < 0f)
		{
			this.Destroy();
		}
		this.vel *= 0.7f;
		this.vel += Custom.DegToVec(this.dir) * UnityEngine.Random.value * 2f;
		this.dir += Mathf.Lerp(-17f, 17f, UnityEngine.Random.value);
		this.graphic = !this.graphic;
		if (this.room?.GetTile(this.pos).Terrain == Room.Tile.TerrainType.Solid)
		{
			if (this.room.GetTile(this.lastPos).Terrain != Room.Tile.TerrainType.Solid)
			{
				this.vel *= 0f;
				this.pos = this.lastPos;
			}
			else
			{
				this.Destroy();
			}
		}
	}

	// Token: 0x06001EBC RID: 7868 RVA: 0x001C565E File Offset: 0x001C385E
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("pixel", true);
		sLeaser.sprites[0].color = this.color;
		// this.AddToContainer(sLeaser, rCam, null);
		// this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Background"));
		this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
		//BackgroundShortcuts
		
	}

	// Token: 0x06001EBD RID: 7869 RVA: 0x001C569C File Offset: 0x001C389C
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		sLeaser.sprites[0].x = Mathf.Lerp(this.lastPos.x, this.pos.x, timeStacker) - camPos.x;
		sLeaser.sprites[0].y = Mathf.Lerp(this.lastPos.y, this.pos.y, timeStacker) - camPos.y;
		/*
		if (UnityEngine.Random.value < 0.01f)
		{
			sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName("mouseSparkB");
		}
		else
		{
			sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName((!this.graphic) ? "pixel" : "mouseSparkA");
		}
		if (UnityEngine.Random.value < 0.125f)
		{
			sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
		}
		else
		{
			sLeaser.sprites[0].color = this.color;
		}
		*/
		
		//LETS TINKER WITH THIS A BIT. MAYBE THEY'D LOOK BETTER AS ALL PIXELS
		sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName("pixel");
		sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
		
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	// Token: 0x06001EBE RID: 7870 RVA: 0x0000544D File Offset: 0x0000364D
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
	}

	// Token: 0x06001EBF RID: 7871 RVA: 0x0004E116 File Offset: 0x0004C316
	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		base.AddToContainer(sLeaser, rCam, newContatiner);
	}

	// Token: 0x0400213C RID: 8508
	public float lifeTime;

	// Token: 0x0400213D RID: 8509
	public float life;

	// Token: 0x0400213E RID: 8510
	public Color color;

	// Token: 0x0400213F RID: 8511
	public bool graphic;

	// Token: 0x04002140 RID: 8512
	public float dir;
}
