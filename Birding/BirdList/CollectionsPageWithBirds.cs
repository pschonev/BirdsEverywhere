using Birding.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Birding.BirdList;

internal class CollectionsPageWithBirds : CollectionsPage
{
    private const int TooltipWidth = 320;
    private const int Padding = 20;
    private const float IconScale = 1.5f;
    private const int IconSize = 24;
    private const int IconGap = 8;

    private static readonly Rectangle _boxSource = new(0, 256, 60, 60);
    private static readonly Color _playerColor = new(60, 100, 170);
    private static readonly Color _locationColor = new(50, 120, 70);
    private static readonly Color _springColor = new(70, 140, 50);
    private static readonly Color _summerColor = new(190, 130, 20);
    private static readonly Color _fallColor = new(170, 80, 30);
    private static readonly Color _winterColor = new(70, 120, 190);

    private sealed record BirdTooltip(string Name, string ScientificName, string Description, string Player, string Location, string Date, string Season);

    private BirdTooltip? _birdTooltip;
    private readonly IReflectedField<string> _hoverText;
    private readonly IReflectedField<int> _value;
    private readonly IReflectedField<Item?> _hoverItem;
    private readonly IReadOnlyDictionary<string, BirdDefinition> _catalog;
    private readonly SaveData _saveData;
    private readonly ITranslationHelper _translation;

    public CollectionsPageWithBirds(CollectionsPage cp, IReflectionHelper reflection, ITranslationHelper translation, IReadOnlyDictionary<string, BirdDefinition> catalog, SaveData saveData)
        : base(cp.xPositionOnScreen, cp.yPositionOnScreen, cp.width, cp.height)
    {
        _translation = translation;
        _catalog = catalog;
        _saveData = saveData;
        _hoverText = reflection.GetField<string>(this, "hoverText");
        _value = reflection.GetField<int>(this, "value");
        _hoverItem = reflection.GetField<Item?>(this, "hoverItem");
        backButton = cp.backButton;
        forwardButton = cp.forwardButton;
        sideTabs = cp.sideTabs;
        currentTab = cp.currentTab;
        currentPage = cp.currentPage;
        secretNoteImage = cp.secretNoteImage;
        collections = cp.collections;
        secretNotesData = cp.secretNotesData;
        secretNoteImageTexture = cp.secretNoteImageTexture;
        letterviewerSubMenu = cp.letterviewerSubMenu;
    }

    public override void performHoverAction(int x, int y)
    {
        IReflectedField<string> hoverText = _hoverText;
        IReflectedField<int> value = _value;
        IReflectedField<Item?> hoverItem = _hoverItem;

        hoverText.SetValue("");
        value.SetValue(-1);
        secretNoteImage = -1;
        _birdTooltip = null;

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
                    else if (currentTab == 8)
                    {
                        _birdTooltip = GetBirdTooltip(data_split[0]);
                    }
                    else
                    {
                        hoverText.SetValue(createDescription(data_split[0]));
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

    public override void draw(SpriteBatch b)
    {
        base.draw(b);
        if (_birdTooltip is { } tooltip)
        {
            DrawBirdTooltip(b, tooltip);
            drawMouse(b);
        }
    }

    private BirdTooltip GetBirdTooltip(string id)
    {
        BirdDefinition data = _catalog[id];
        ObservationData obs = _saveData.BirdObservations[id];
        return new BirdTooltip(
            BirdText.Name(_translation, data),
            data.ScientificName,
            Game1.parseText(BirdText.Description(_translation, data), Game1.smallFont, TooltipWidth),
            _translation.Get("ui.first-seen.player", new { player = obs.PlayerName }),
            _translation.Get("ui.first-seen.location", new { location = obs.ObservationLocation }),
            _translation.Get("ui.first-seen.date", new { date = obs.ObservationDate.ToLocaleString() }),
            obs.ObservationDate.Season.ToString().ToLowerInvariant());
    }

    private static (string ItemId, Color Color) SeasonStyle(string season) => season switch
    {
        "spring" => ("(O)18", _springColor),
        "summer" => ("(O)421", _summerColor),
        "fall" => ("(O)276", _fallColor),
        _ => ("(O)416", _winterColor),
    };

    private static void DrawBox(SpriteBatch b, int x, int y, int width, int height) =>
        drawTextureBox(b, Game1.menuTexture, _boxSource, x, y, width, height, Color.White, 1f, drawShadow: false);

    private static void DrawRow(SpriteBatch b, int x, int y, int rowHeight, string text, Color color, string? itemId)
    {
        if (itemId is null)
        {
            b.Draw(Game1.staminaRect, new Rectangle(x + (IconSize / 2) - 4, y + (rowHeight / 2) - 4, 8, 8), color);
        }
        else
        {
            var data = ItemRegistry.GetDataOrErrorItem(itemId);
            b.Draw(data.GetTexture(), new Vector2(x, y + ((rowHeight - IconSize) / 2f)), data.GetSourceRect(), Color.White, 0f, Vector2.Zero, IconScale, SpriteEffects.None, 1f);
        }
        Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(x + IconSize + IconGap, y + ((rowHeight - Game1.smallFont.MeasureString(text).Y) / 2f)), color);
    }

    private static void DrawBirdTooltip(SpriteBatch b, BirdTooltip tip)
    {
        (string seasonItem, Color seasonColor) = SeasonStyle(tip.Season);
        Vector2 nameSize = Game1.dialogueFont.MeasureString(tip.Name);
        Vector2 scientificSize = Game1.smallFont.MeasureString(tip.ScientificName);
        Vector2 descriptionSize = Game1.smallFont.MeasureString(tip.Description);
        int rowHeight = Math.Max(IconSize, Game1.smallFont.LineSpacing) + 4;
        float rowsWidth = IconSize + IconGap + Math.Max(Game1.smallFont.MeasureString(tip.Player).X, Math.Max(Game1.smallFont.MeasureString(tip.Location).X, Game1.smallFont.MeasureString(tip.Date).X));
        int contentWidth = (int)Math.Max(TooltipWidth, Math.Max(nameSize.X, Math.Max(scientificSize.X, rowsWidth)));
        int boxWidth = contentWidth + (2 * Padding);
        int headerHeight = (int)(nameSize.Y + scientificSize.Y) + (2 * Padding);
        int descriptionHeight = (int)descriptionSize.Y + (2 * Padding);
        int footerHeight = (3 * rowHeight) + (2 * Padding);
        int totalHeight = headerHeight + descriptionHeight + footerHeight;

        int x = Math.Max(0, Math.Min(Game1.getMouseX() + 32, Game1.uiViewport.Width - boxWidth));
        int y = Math.Max(0, Math.Min(Game1.getMouseY() + 32, Game1.uiViewport.Height - totalHeight));

        DrawBox(b, x, y, boxWidth, headerHeight);
        DrawBox(b, x, y + headerHeight, boxWidth, descriptionHeight);
        DrawBox(b, x, y + headerHeight + descriptionHeight, boxWidth, footerHeight);

        Utility.drawTextWithShadow(b, tip.Name, Game1.dialogueFont, new Vector2(x + Padding, y + Padding), Game1.textColor);
        b.DrawString(Game1.smallFont, tip.ScientificName, new Vector2(x + Padding, y + Padding + nameSize.Y), Game1.textColor * 0.6f);
        b.DrawString(Game1.smallFont, tip.Description, new Vector2(x + Padding, y + headerHeight + Padding), Game1.textColor);

        int rowX = x + Padding;
        int rowY = y + headerHeight + descriptionHeight + Padding;
        DrawRow(b, rowX, rowY, rowHeight, tip.Player, _playerColor, null);
        DrawRow(b, rowX, rowY + rowHeight, rowHeight, tip.Location, _locationColor, null);
        DrawRow(b, rowX, rowY + (2 * rowHeight), rowHeight, tip.Date, seasonColor, seasonItem);
    }
}
