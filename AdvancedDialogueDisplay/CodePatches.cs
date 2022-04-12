﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Menus;
using System;

namespace AdvancedDialogueDisplay
{
    public partial class ModEntry
    {
		private static bool preventGetCurrentString;
		[HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.drawPortrait))]
        public class DialogueBox_drawPortrait_Patch
        {
            public static bool Prefix(DialogueBox __instance, SpriteBatch b)
            {
                if (!Config.EnableMod)
                    return true;
				string name = __instance.characterDialogue.speaker.getName();
				//name = "asdfkhdsafhfdsk";
				if (!dataDict.TryGetValue(name, out DialogueDisplayData data))
					data = dataDict[defaultKey];


				// Images

				var images = data.images is null ? dataDict[defaultKey].images : data.images;

				foreach (var image in images)
                {
					b.Draw(imageDict[image.texturePath], GetDataVector(__instance, image), new Rectangle(image.x, image.y, image.w, image.h), Color.White * image.alpha, 0, Vector2.Zero, image.scale, SpriteEffects.None, image.layerDepth);
				}


				// NPC Portrait

				var portrait = data.portrait is null ? dataDict[defaultKey].portrait : data.portrait;

                if (!portrait.disabled)
                {
					Texture2D portraitTexture;
					Rectangle portraitSource;

					if (portrait.texturePath != null)
					{
						portraitTexture = imageDict[portrait.texturePath];
					}
					else
					{
						if (__instance.characterDialogue.overridePortrait != null)
							portraitTexture = __instance.characterDialogue.overridePortrait;
						else
							portraitTexture = __instance.characterDialogue.speaker.Portrait;
					}
					if (!portrait.tileSheet)
					{
						portraitSource = new Rectangle(0, 0, portrait.w, portrait.h);
					}
					else
					{
						portraitSource = Game1.getSourceRectForStandardTileSheet(portraitTexture, __instance.characterDialogue.getPortraitIndex(), portrait.w, portrait.h);
					}
					if (!portraitTexture.Bounds.Contains(portraitSource))
					{
						portraitSource = new Rectangle(0, 0, portrait.w, portrait.h);
					}


					int xOffset = (bool)AccessTools.Method(typeof(DialogueBox), "shouldPortraitShake").Invoke(__instance, new object[] { __instance.characterDialogue }) ? Game1.random.Next(-1, 2) : 0;
					b.Draw(portraitTexture, GetDataVector(__instance, portrait) + new Vector2(xOffset, 0), new Rectangle?(portraitSource), Color.White * portrait.alpha, 0f, Vector2.Zero, portrait.scale, SpriteEffects.None, portrait.layerDepth);
				}



				// NPC Name

				var npcName = data.name != null ? data.name : dataDict[defaultKey].name;
                if (!npcName.disabled)
                {
					var namePos = GetDataVector(__instance, npcName);

					if (npcName.centered)
					{
						if (npcName.scroll)
						{
							SpriteText.drawStringWithScrollCenteredAt(b, name, (int)namePos.X, (int)namePos.Y, npcName.placeholderText is null ? name : npcName.placeholderText, npcName.alpha, npcName.color, npcName.scrollType, npcName.layerDepth, npcName.junimo);
						}
						else
						{
							SpriteText.drawStringHorizontallyCenteredAt(b, name, (int)namePos.X, (int)namePos.Y, 999999, npcName.width, 999999, npcName.alpha, npcName.layerDepth, npcName.junimo, npcName.color);
						}

					}
					else
					{
						if (npcName.right)
							namePos.X -= SpriteText.getWidthOfString(name);

						if (npcName.scroll)
						{
							SpriteText.drawStringWithScrollBackground(b, name, (int)namePos.X, (int)namePos.Y, npcName.placeholderText is null ? name : npcName.placeholderText, npcName.alpha, npcName.color, npcName.alignment);
						}
						else
						{
							SpriteText.drawString(b, name, (int)namePos.X, (int)namePos.Y, 999999, npcName.width, 999999, npcName.alpha, npcName.layerDepth, npcName.junimo, npcName.color);
						}
					}
				}


				// Texts

				var texts = data.texts is null ? dataDict[defaultKey].texts : data.texts;

				foreach (var text in texts)
                {
					var pos = GetDataVector(__instance, text);
					if (text.centered)
					{
						if (text.variable && text.right)
							pos.X -= SpriteText.getWidthOfString(text.text) / 2;

						if (text.scroll)
						{
							SpriteText.drawStringWithScrollCenteredAt(b, name, (int)pos.X, (int)pos.Y, text.placeholderText, text.alpha, text.color, text.scrollType, text.layerDepth, text.junimo);
						}
						else
						{
							SpriteText.drawStringHorizontallyCenteredAt(b, name, (int)pos.X, (int)pos.Y, 999999, text.width, 999999, text.alpha, text.layerDepth, text.junimo, text.color);
						}

					}
					else
					{
						if (text.variable && text.right)
							pos.X -= SpriteText.getWidthOfString(text.text);

						if (text.scroll)
						{
							SpriteText.drawStringWithScrollBackground(b, name, (int)pos.X, (int)pos.Y, text.placeholderText, text.alpha, text.color, text.alignment);
						}
						else
						{
							SpriteText.drawString(b, name, (int)pos.X, (int)pos.Y, 999999, text.width, 999999, text.alpha, text.layerDepth, text.junimo, text.color);
						}
					}
				}

                if (Game1.player.friendshipData.ContainsKey(name))
                {

					var hearts = data.hearts is null ? dataDict[defaultKey].hearts : data.hearts;
					if (hearts is not null && !hearts.disabled)
					{
						var pos = GetDataVector(__instance, hearts);
						int heartLevel = Game1.player.getFriendshipHeartLevelForNPC(name);
						int extraFriendshipPixels = Game1.player.getFriendshipLevelForNPC(name) % 250;

						bool datable = SocialPage.isDatable(name);
						bool spouse = false;
						if (Game1.player.friendshipData.TryGetValue(name, out Friendship friendship))
						{
							spouse = friendship.IsMarried();
						}
						for (int h = 0; h < Math.Max(Utility.GetMaximumHeartsForCharacter(Game1.getCharacterFromName(name, true, false)), 10); h++)
						{
							if (h > heartLevel && !hearts.showEmptyHearts)
								break;
							if (h == heartLevel && extraFriendshipPixels == 0)
								break;
							int xSource = (h < heartLevel) ? 211 : 218;
							if (datable && !friendship.IsDating() && !spouse && h >= 8)
							{
								xSource = 211;
							}
							int x = h % hearts.heartsPerRow;
							int y = h / hearts.heartsPerRow;
							b.Draw(Game1.mouseCursors, pos + new Vector2(x * 32, y * 32), new Rectangle?(new Rectangle(xSource, 428, 7, 6)), (datable && !friendship.IsDating() && !spouse && h >= 8) ? (Color.Black * 0.35f) : Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
							if (h == heartLevel && extraFriendshipPixels > 0)
							{
								b.Draw(Game1.mouseCursors, pos + new Vector2(x * 32, y * 32), new Rectangle?(new Rectangle(211, 428, (int)Math.Round(7 * (extraFriendshipPixels / 250f)), 6)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
							}
						}
					}

					var gifts = data.gifts is null ? dataDict[defaultKey].gifts : data.gifts;
					if (gifts is not null && !gifts.disabled && !Game1.player.friendshipData[name].IsMarried() && Game1.getCharacterFromName(name) is not Child)
					{
						var pos = GetDataVector(__instance, gifts);
						Utility.drawWithShadow(b, Game1.mouseCursors2, pos + new Vector2(6, 0), new Rectangle(166, 174, 14, 12), Color.White, 0f, Vector2.Zero, 4f, false, 0.88f, 0, -1, 0.2f);
						b.Draw(Game1.mouseCursors, pos + (gifts.inline ? new Vector2(64, 8) : new Vector2(0, 56)), new Rectangle?(new Rectangle(227 + ((Game1.player.friendshipData[name].GiftsThisWeek >= 2) ? 9 : 0), 425, 9, 9)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
						b.Draw(Game1.mouseCursors, pos + (gifts.inline ? new Vector2(96, 8) : new Vector2(32, 56)), new Rectangle?(new Rectangle(227 + (Game1.player.friendshipData[name].GiftsThisWeek >= 1 ? 9 : 0), 425, 9, 9)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
					}

					// Jewel

					if (__instance.shouldDrawFriendshipJewel())
					{
						var jewel = data.jewel is null ? dataDict[defaultKey].jewel : data.jewel;
						if (jewel != null && !jewel.disabled)
						{
							var pos = GetDataVector(__instance, jewel);
							b.Draw(Game1.mouseCursors, pos, new Rectangle?((Game1.player.getFriendshipHeartLevelForNPC(__instance.characterDialogue.speaker.Name) >= 10) ? new Rectangle(269, 494, 11, 11) : new Rectangle(Math.Max(140, 140 + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000.0 / 250.0) * 11), Math.Max(532, 532 + Game1.player.getFriendshipHeartLevelForNPC(__instance.characterDialogue.speaker.Name) / 2 * 11), 11, 11)), Color.White * jewel.alpha, 0f, Vector2.Zero, jewel.scale, SpriteEffects.None, jewel.layerDepth);
						}
					}
				}



				// Dialogue String

				var dialogue = data.dialogue is null ? dataDict[defaultKey].dialogue : data.dialogue;
				var dialoguePos = GetDataVector(__instance, dialogue);
				preventGetCurrentString = false;
				SpriteText.drawString(b, __instance.getCurrentString(), (int)dialoguePos.X, (int)dialoguePos.Y, __instance.characterIndexInDialogue, dialogue.width >= 0 ? dialogue.width : __instance.width - 8, 999999, dialogue.alpha, dialogue.layerDepth, false, -1, "", dialogue.color, dialogue.alignment);


				// Close Icon

				if(__instance.dialogueIcon != null)
                {
					var button = data.button is null ? dataDict[defaultKey].button : data.button;

					if(button != null && !button.disabled)
						__instance.dialogueIcon.position = GetDataVector(__instance, button);
				}


				// dividers

				var dividers = data.dividers is null ? dataDict[defaultKey].dividers : data.dividers;

				if(dividers != null)
                {
					foreach (var divider in dividers)
					{
						if (divider.horizontal)
						{
							DrawHorizontalPartition(b, __instance, divider);
						}
						else
						{
							DrawVerticalPartition(b, __instance, divider);
						}

					}
				}

				preventGetCurrentString = true;
				return false;
			}

            private static void DrawHorizontalPartition(SpriteBatch b, DialogueBox box, DividerData divider)
			{

				Color tint = (divider.red == -1) ? Color.White : new Color(divider.red, divider.green, divider.blue);
				Texture2D texture = (divider.red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture;
				b.Draw(texture, new Rectangle(box.x + divider.xOffset, box.y + divider.yOffset, divider.width, 64), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 6, -1, -1)), tint);
			}
            private static void DrawVerticalPartition(SpriteBatch b, DialogueBox instance, DividerData divider)
			{
				Color tint = (divider.red == -1) ? Color.White : new Color(divider.red, divider.green, divider.blue);
				b.Draw(Game1.mouseCursors, new Rectangle(instance.x + divider.xOffset, instance.y + divider.yOffset, 36, divider.height), new Rectangle?(new Rectangle(278, 324, 9, 1)), tint);

			}

			private static Vector2 GetDataVector(DialogueBox box, BaseData data)
            {
				return new Vector2(box.x + (data.right ? box.width : 0) + data.xOffset, box.y + (data.bottom ? box.height : 0) + data.yOffset);
			}
        }
        [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.getCurrentString))]
        public class DialogueBox_getCurrentString_Patch
		{
            public static bool Prefix(DialogueBox __instance, ref string __result)
            {
                if (!Config.EnableMod || !preventGetCurrentString)
                    return true;
				__result = "";
				return false;
			}

		}
		[HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.draw))]
		public class DialogueBox_draw_Patch
		{
			public static void Postfix(DialogueBox __instance, SpriteBatch b)
			{
				if (!Config.EnableMod)
					return;

				preventGetCurrentString = false;
			}

		}
	}
}