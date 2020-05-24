using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Targeting;

namespace Server.Commands.Generic
{
  public class DesignInsertCommand : BaseCommand
  {
    public enum DesignInsertResult
    {
      Valid,
      InvalidItem,
      NotInHouse,
      OutsideHouseBounds
    }

    public DesignInsertCommand()
    {
      AccessLevel = AccessLevel.GameMaster;
      Supports = CommandSupport.Single | CommandSupport.Area;
      Commands = new[] { "DesignInsert" };
      ObjectTypes = ObjectTypes.Items;
      Usage = "DesignInsert [allItems=false]";
      Description = "Inserts multiple targeted items into a customizable house's design.";
    }

    public static void Initialize()
    {
      TargetCommands.Register(new DesignInsertCommand());
    }

    public static DesignInsertResult ProcessInsert(Item item, bool staticsOnly, out HouseFoundation house)
    {
      house = null;

      if (item == null || item is BaseMulti || item is HouseSign || (staticsOnly && !(item is Static)))
        return DesignInsertResult.InvalidItem;

      house = BaseHouse.FindHouseAt(item) as HouseFoundation;

      if (house == null)
        return DesignInsertResult.NotInHouse;

      int x = item.X - house.X;
      int y = item.Y - house.Y;
      int z = item.Z - house.Z;

      if (!TryInsertIntoState(house.CurrentState, item.ItemID, x, y, z))
        return DesignInsertResult.OutsideHouseBounds;

      TryInsertIntoState(house.DesignState, item.ItemID, x, y, z);
      item.Delete();

      return DesignInsertResult.Valid;
    }

    private static bool TryInsertIntoState(DesignState state, int itemID, int x, int y, int z)
    {
      MultiComponentList mcl = state.Components;

      if (x < mcl.Min.X || y < mcl.Min.Y || x > mcl.Max.X || y > mcl.Max.Y)
        return false;

      mcl.Add(itemID, x, y, z);
      state.OnRevised();

      return true;
    }

    public override void Execute(CommandEventArgs e, object obj)
    {
      Target t = new DesignInsertTarget(new List<HouseFoundation>(), e.Length < 1 || !e.GetBoolean(0));
      t.Invoke(e.Mobile, obj);
    }

    private class DesignInsertTarget : Target
    {
      private readonly List<HouseFoundation> m_Foundations;
      private readonly bool m_StaticsOnly;

      public DesignInsertTarget(List<HouseFoundation> foundations, bool staticsOnly)
        : base(-1, false, TargetFlags.None)
      {
        m_Foundations = foundations;
        m_StaticsOnly = staticsOnly;
      }

      protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
      {
        if (m_Foundations.Count != 0)
        {
          from.SendMessage("Your changes have been committed. Updating...");

          foreach (HouseFoundation house in m_Foundations)
            house.Delta(ItemDelta.Update);
        }
      }

      protected override void OnTarget(Mobile from, object obj)
      {
        DesignInsertResult result = ProcessInsert(obj as Item, m_StaticsOnly, out HouseFoundation house);

        switch (result)
        {
          case DesignInsertResult.Valid:
            {
              if (m_Foundations.Count == 0)
                from.SendMessage(
                  "The item has been inserted into the house design. Press ESC when you are finished.");
              else
                from.SendMessage("The item has been inserted into the house design.");

              if (!m_Foundations.Contains(house))
                m_Foundations.Add(house);

              break;
            }
          case DesignInsertResult.InvalidItem:
            {
              from.SendMessage("That cannot be inserted. Try again.");
              break;
            }
          case DesignInsertResult.NotInHouse:
          case DesignInsertResult.OutsideHouseBounds:
            {
              from.SendMessage("That item is not inside a customizable house. Try again.");
              break;
            }
        }

        from.Target = new DesignInsertTarget(m_Foundations, m_StaticsOnly);
      }
    }

    public override void ExecuteList(CommandEventArgs e, List<object> list)
    {
      Mobile from = e.Mobile;
      from.SendGump(new WarningGump(1060637, 30720,
        $"You are about to insert {list.Count} objects. This cannot be undone without a full server revert.<br><br>Continue?",
        0xFFC000, 420, 280, okay => OnConfirmCallback(from, okay, list, e.Length < 1 || !e.GetBoolean(0))));
      AddResponse("Awaiting confirmation...");
    }

    private void OnConfirmCallback(Mobile from, bool okay, List<object> list, bool staticsOnly)
    {
      bool flushToLog = false;

      if (okay)
      {
        List<HouseFoundation> foundations = new List<HouseFoundation>();
        flushToLog = list.Count > 20;

        for (int i = 0; i < list.Count; ++i)
        {
          DesignInsertResult result = ProcessInsert(list[i] as Item, staticsOnly, out HouseFoundation house);

          switch (result)
          {
            case DesignInsertResult.Valid:
              {
                AddResponse("The item has been inserted into the house design.");

                if (!foundations.Contains(house))
                  foundations.Add(house);

                break;
              }
            case DesignInsertResult.InvalidItem:
              {
                LogFailure("That cannot be inserted.");
                break;
              }
            case DesignInsertResult.NotInHouse:
            case DesignInsertResult.OutsideHouseBounds:
              {
                LogFailure("That item is not inside a customizable house.");
                break;
              }
          }
        }

        foreach (HouseFoundation house in foundations)
          house.Delta(ItemDelta.Update);
      }
      else
      {
        AddResponse("Command aborted.");
      }

      Flush(from, flushToLog);
    }
  }
}