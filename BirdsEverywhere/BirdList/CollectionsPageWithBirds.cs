using System;
using StardewValley.Menus;
using StardewValley;
using StardewModdingAPI;

namespace BirdsEverywhere.BirdList
{
    class CollectionsPageWithBirds : CollectionsPage
    {
		public CollectionsPageWithBirds(CollectionsPage cp)
			: base(cp.xPositionOnScreen, cp.yPositionOnScreen, cp.width, cp.height)
        {
			this.backButton = cp.backButton;
			this.forwardButton = cp.forwardButton;
			this.sideTabs = cp.sideTabs;
			this.currentTab = cp.currentTab;
			this.currentPage = cp.currentPage;
			this.secretNoteImage = cp.secretNoteImage;
			this.collections = cp.collections;
			this.secretNotesData = cp.secretNotesData;
			this.secretNoteImageTexture = cp.secretNoteImageTexture;
			this.letterviewerSubMenu = cp.letterviewerSubMenu;
	}

		public override void performHoverAction(int x, int y)
		{
			IReflectedField<string> hoverText = ModEntry.modInstance.Helper.Reflection.GetField<string>(this, "hoverText");
			IReflectedField<string> descriptionText = ModEntry.modInstance.Helper.Reflection.GetField<string>(this, "descriptionText");
			IReflectedField<int> value = ModEntry.modInstance.Helper.Reflection.GetField<int>(this, "value");
			IReflectedField<Item> hoverItem = ModEntry.modInstance.Helper.Reflection.GetField<Item>(this, "hoverItem");

			descriptionText.SetValue("");
			hoverText.SetValue("");
			value.SetValue(-1);
			secretNoteImage = -1;

			if (letterviewerSubMenu != null)
			{
				letterviewerSubMenu.performHoverAction(x, y);
				return;
			}
			foreach (ClickableTextureComponent c2 in sideTabs.Values)
			{
				if (c2.containsPoint(x, y))
				{
					hoverText.SetValue(c2.hoverText);
					return;
				}
			}
			bool hoveredAny = false;
			foreach (ClickableTextureComponent c in collections[currentTab][currentPage])
			{
				if (c.containsPoint(x, y))
				{
					c.scale = Math.Min(c.scale + 0.02f, c.baseScale + 0.1f);
					string[] data_split = c.name.Split(' ');
					if (currentTab == 5 || (data_split.Length > 1 && Convert.ToBoolean(data_split[1])) || (data_split.Length > 2 && Convert.ToBoolean(data_split[2])))
					{
						if (currentTab == 7)
						{
							hoverText.SetValue(Game1.parseText(c.name.Substring(c.name.IndexOf(' ', c.name.IndexOf(' ') + 1) + 1), Game1.smallFont, 256));
						}
						else if (currentTab == 8) {
							// this is my bird tab
							hoverText.SetValue(getBirdHoverText(c.name.Split(null)[0]));
                        }
						else
						{
							hoverText.SetValue(createDescription(Convert.ToInt32(data_split[0])));
						}
					}
					else
					{
						if (hoverText.GetValue() != "???")
						{
							hoverItem.SetValue(null);
						}
						hoverText.SetValue("???");
					}
					hoveredAny = true;
				}
				else
				{
					c.scale = Math.Max(c.scale - 0.02f, c.baseScale);
				}
			}
			if (!hoveredAny)
			{
				hoverItem.SetValue(null);
			}
			forwardButton.tryHover(x, y, 0.5f);
			backButton.tryHover(x, y, 0.5f);
		}

        private string getBirdHoverText(string id)
        {
			var data = ModEntry.birdDataCollection[id];
			var obsData = ModEntry.saveData.birdObservations[id];
			return $"{data.name}\n{data.scName}\n\n{data.description}\n\nFirst seen by {obsData.playerName}\n{obsData.observationLocation} on {obsData.observationDate}";
        }
    }
}
