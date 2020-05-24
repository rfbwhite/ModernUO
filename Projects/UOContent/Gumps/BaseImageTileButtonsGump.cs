using System.Collections.Generic;
using Server.Network;

namespace Server.Gumps
{
  public class ImageTileButtonInfo
  {
    private int m_Hue;
    private int m_ItemID;

    private TextDefinition m_Label;
    private int m_LocalizedTooltip;

    public ImageTileButtonInfo(int itemID, int hue, TextDefinition label, int localizedTooltip = -1)
    {
      m_Hue = hue;
      m_ItemID = itemID;
      m_Label = label;
      m_LocalizedTooltip = localizedTooltip;
    }

    public virtual int ItemID
    {
      get => m_ItemID;
      set => m_ItemID = value;
    }

    public virtual int Hue
    {
      get => m_Hue;
      set => m_Hue = value;
    }

    public virtual int LocalizedTooltip
    {
      get => m_LocalizedTooltip;
      set => m_LocalizedTooltip = value;
    }

    public virtual TextDefinition Label
    {
      get => m_Label;
      set => m_Label = value;
    }
  }

  public class BaseImageTileButtonsGump : Gump
  {
    public BaseImageTileButtonsGump(TextDefinition header, List<ImageTileButtonInfo> buttons) : this(header, buttons.ToArray())
    {
    }

    public BaseImageTileButtonsGump(TextDefinition header, ImageTileButtonInfo[] buttons) : base(10, 10) // Coords are 0, o on OSI, intentional difference
    {
      Buttons = buttons;
      AddPage(0);

      int x = XItems * 250;
      int y = YItems * 64;

      AddBackground(0, 0, x + 20, y + 84, 0x13BE);
      AddImageTiled(10, 10, x, 20, 0xA40);
      AddImageTiled(10, 40, x, y + 4, 0xA40);
      AddImageTiled(10, y + 54, x, 20, 0xA40);
      AddAlphaRegion(10, 10, x, y + 64);

      AddButton(10, y + 54, 0xFB1, 0xFB2, 0); // Cancel Button
      AddHtmlLocalized(45, y + 56, x - 50, 20, 1060051, 0x7FFF); // CANCEL
      TextDefinition.AddHtmlText(this, 14, 12, x, 20, header, false, false, 0x7FFF, 0xFFFFFF);

      AddPage(1);

      int itemsPerPage = XItems * YItems;

      for (int i = 0; i < buttons.Length; i++)
      {
        int position = i % itemsPerPage;

        int innerX = position % XItems * 250 + 14;
        int innerY = position / XItems * 64 + 44;

        int pageNum = i / itemsPerPage + 1;

        if (position == 0 && i != 0)
        {
          AddButton(x - 100, y + 54, 0xFA5, 0xFA7, 0, GumpButtonType.Page, pageNum);
          AddHtmlLocalized(x - 60, y + 56, 60, 20, 1043353, 0x7FFF); // Next

          AddPage(pageNum);

          AddButton(x - 200, y + 54, 0xFAE, 0xFB0, 0, GumpButtonType.Page, pageNum - 1);
          AddHtmlLocalized(x - 160, y + 56, 60, 20, 1011393, 0x7FFF); // Back
        }

        ImageTileButtonInfo b = buttons[i];

        AddImageTiledButton(innerX, innerY, 0x918, 0x919, 100 + i, GumpButtonType.Reply, 0, b.ItemID, b.Hue, 15, 10,
          b.LocalizedTooltip);
        TextDefinition.AddHtmlText(this, innerX + 84, innerY, 250, 60, b.Label, false, false, 0x7FFF, 0xFFFFFF);
      }
    }

    protected ImageTileButtonInfo[] Buttons { get; }

    protected virtual int XItems => 2;
    protected virtual int YItems => 5;

    public override void OnResponse(NetState sender, RelayInfo info)
    {
      int adjustedID = info.ButtonID - 100;

      if (adjustedID >= 0 && adjustedID < Buttons.Length)
        HandleButtonResponse(sender, adjustedID, Buttons[adjustedID]);
      else
        HandleCancel(sender);
    }

    public virtual void HandleButtonResponse(NetState sender, int adjustedButton, ImageTileButtonInfo buttonInfo)
    {
    }

    public virtual void HandleCancel(NetState sender)
    {
    }
  }
}