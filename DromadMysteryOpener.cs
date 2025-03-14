using System;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
    [Serializable]
    public class DromadMysteryOpener : IPart
    {
        [NonSerialized]
        public static string[] ColorList = new string[] { "&R", "&G", "&B", "&M", "&Y", "&W" };

        // Register events
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Object.RegisterPartEvent(this, "GetInventoryActions");
            Object.RegisterPartEvent(this, "InvCommandPrize");
            base.Register(Object, Registrar);
        }

        // Handle events
        public override bool FireEvent(Event E)
        {
            if (E.ID == "GetInventoryActions")
            {
                HandleGetInventoryActions(E);
            }
            if (E.ID == "InvCommandPrize")
            {
                HandleInvCommandPrize(E);
            }

            return true;
        }

        private void HandleGetInventoryActions(Event E)
        {
            if (IPart.ThePlayer.OnWorldMap()) return;

            EventParameterGetInventoryActions actions = E.GetParameter("Actions") as EventParameterGetInventoryActions;
            actions.AddAction("Prize", 'o', false, "...&Wo&ypen?", "InvCommandPrize", "highligh?", 2, 0, false, false, false, false);
        }

        private void HandleInvCommandPrize(Event E)
        {
            Popup.Show("You cautiously pry open the " + this.ParentObject.DisplayName + ". It vanishes in an explosion of confetti!");
            E.RequestInterfaceExit();
            SpawnPrize();
        }

        public GameObject SpawnPrize()
        {
            Cell cell = GetValidCellForPrize();

            if (cell == null)
            {
                Popup.Show("There's nowhere to put your prize.");
                return null;
            }

            GameObject prize = CreatePrize();
            if (prize != null)
            {
                DisplayPrizePopup(prize);
                DestroyParentObject();
                PlacePrizeInCell(prize, cell);
            }

            return prize;
        }

        private Cell GetValidCellForPrize()
        {
            Cell cell = GetCellFromParentObject();

            if (cell == null)
            {
                cell = GetCellFromInventory();
            }

            if (cell == null)
            {
                cell = GetCellFromEquippedObject();
            }

            return cell;
        }

        private Cell GetCellFromParentObject()
        {
            return this.ParentObject.CurrentCell;
        }

        private Cell GetCellFromInventory()
        {
            GameObject host = this.ParentObject.InInventory;
            return host?.CurrentCell;
        }

        private Cell GetCellFromEquippedObject()
        {
            GameObject host = this.ParentObject.Equipped;
            return host?.CurrentCell;
        }

        private GameObject CreatePrize()
        {
            GameObjectBlueprint randomElement = GetRandomValidBlueprint();
            if (randomElement == null) return null;

            return GameObjectFactory.Factory.CreateObject(randomElement.Name);
        }

        private GameObjectBlueprint GetRandomValidBlueprint()
        {
            GameObjectBlueprint randomElement;
            int num = 0;

            // Try to find a valid prize
            for (;;)
            {
                randomElement = GameObjectFactory.Factory.BlueprintList.GetRandomElement(null);
                if (IsValidPrize(randomElement)) break;

                if (++num > 10000)
                {
                    return null;
                }
            }

            return randomElement;
        }

        private bool IsValidPrize(GameObjectBlueprint randomElement)
        {
            return randomElement.HasPart("Physics") &&
                   randomElement.HasPart("Render") &&
                   !randomElement.Tags.ContainsKey("BaseObject") &&
                   !randomElement.Tags.ContainsKey("Terrain") &&
                   //randomElement.ResolvePartParameter("Physics", "IsReal", string.Empty).ToUpper() != "FALSE" &&
                   //!randomElement.ResolvePartParameter("Render", "DisplayName", string.Empty).Contains("[") &&
                   (!randomElement.Props.ContainsKey("SparkingQuestBlueprint") || randomElement.Name == randomElement.Props["SparkingQuestBlueprint"]) &&
                   !randomElement.Tags.ContainsKey("NoDromadMysteryBox");
        }

        private void DisplayPrizePopup(GameObject prize)
        {
            Popup.Show("You have won " + (prize.a + prize.DisplayNameOnlyStripped).ToUpper() + "!");
        }

        private void DestroyParentObject()
        {
            this.ParentObject.Destroy("prizes!", true);
        }

        private void PlacePrizeInCell(GameObject prize, Cell cell)
        {
            if (!cell.IsEmpty())
            {
                Cell nearby = cell.GetLocalEmptyAdjacentCells().GetRandomElement(null);
                if (nearby != null) cell = nearby;
            }

            if (prize.Brain != null)
            {
                XRLCore.Core.Game.ActionManager.AddActiveObject(prize);
            }

            cell.AddObject(prize);
            AddPrizeParticleEffects(cell);
            PlayPrizeSound();
        }

        private void AddPrizeParticleEffects(Cell cell)
        {
            for (int i = 0; i < 32; i++)
            {
                XRLCore.ParticleManager.Add(
                    Burgeoning.GetRandomRainbowColor() + "\u0007\u00f8\u00f9".Substring(Stat.RandomCosmetic(0, 2), 1),
                    (float)cell.X, (float)cell.Y,
                    0.13f * (float)Stat.RandomCosmetic(-6, 6),
                    -0.7f + 0.1f * (float)Stat.RandomCosmetic(-6, 6),
                    999, 0f, 0.015f);
            }
        }

        private void PlayPrizeSound()
        {
            IPart.ThePlayer.Physics.PlayWorldSound("party");
        }
    }
}
