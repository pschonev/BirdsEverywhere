using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;

namespace BirdsEverywhere.BirdList;

public static class BirdCollectionsTab
{
    private const int TabId = 8;
    private const int CollectionWidth = 10;

    public static void Install(GameMenu menu, IModContentHelper content, IReflectionHelper reflection, IReadOnlyDictionary<string, BirdData> catalog, SaveData saveData)
    {
        int pageIndex = menu.pages.FindIndex(x => x is CollectionsPage);
        if (pageIndex < 0 || menu.pages[pageIndex] is not CollectionsPage original)
        {
            return;
        }

        CollectionsPageWithBirds cp = new(original, reflection, catalog, saveData);

        Texture2D tabIcon = content.Load<Texture2D>("assets/collections_tab_icon.png");
        cp.sideTabs.Add(TabId, new ClickableTextureComponent(TabId.ToString(CultureInfo.InvariantCulture),
            new Rectangle(cp.xPositionOnScreen - 48, cp.yPositionOnScreen + 64 * (2 + cp.sideTabs.Count), 64, 64), "",
            "Birds", tabIcon, new Rectangle(0, 0, 16, 16), 4f)
        {
            myID = 7009,
            upNeighborID = -99998,
            downNeighborID = -99998,
            rightNeighborID = 0
        });

        List<List<ClickableTextureComponent>> pages = new();
        cp.collections.Add(TabId, pages);

        int widthUsed = 0;
        int baseX = cp.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder;
        int baseY = cp.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - 16;

        foreach (BirdData bird in catalog.Values)
        {
            bool seen = saveData.seenBirds.Contains(bird.id);
            Texture2D birdTexture = content.Load<Texture2D>($"assets/{bird.id}/{bird.id}_icon.png");

            int x = baseX + widthUsed % CollectionWidth * 68;
            int y = baseY + widthUsed / CollectionWidth * 68;
            if (y > cp.yPositionOnScreen + cp.height - 128)
            {
                pages.Add(new List<ClickableTextureComponent>());
                widthUsed = 0;
                x = baseX;
                y = baseY;
            }
            if (pages.Count == 0)
            {
                pages.Add(new List<ClickableTextureComponent>());
            }

            List<ClickableTextureComponent> page = pages[^1];
            int count = page.Count;
            page.Add(new ClickableTextureComponent($"{bird.id} {seen}",
                new Rectangle(x, y, 64, 64), null, "", birdTexture,
                new Rectangle(0, 0, 16, 16), 4f, seen)
            {
                myID = count,
                rightNeighborID = (count + 1) % CollectionWidth == 0 ? -1 : count + 1,
                leftNeighborID = count % CollectionWidth == 0 ? 7001 : count - 1,
                downNeighborID = y + 68 > cp.yPositionOnScreen + cp.height - 128 ? -7777 : count + CollectionWidth,
                upNeighborID = count < CollectionWidth ? 12345 : count - CollectionWidth,
                fullyImmutable = true
            });
            widthUsed++;
        }

        menu.pages[pageIndex] = cp;
    }
}
