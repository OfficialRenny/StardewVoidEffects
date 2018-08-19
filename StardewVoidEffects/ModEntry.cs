using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewModdingAPI.Framework;
using StardewValley;
using SpaceCore;
using SpaceCore.Events;
using System.Linq;

namespace StardewVoidEffects
{

    public class ModEntry : Mod
    {
        bool hasEatenVoid;

        public override void Entry(IModHelper helper)
        {
            SpaceEvents.OnItemEaten += this.SpaceEvents_ItemEaten;
            TimeEvents.AfterDayStarted += this.TimeEvents_DayAdvance;
            helper.ConsoleCommands.Add("void_tolerance", "Checks how many void items you have consumed.", this.Void_Tolerance);
            GameEvents.OneSecondTick += this.Void_Drain;
        }

        private void Void_Drain(object sender, EventArgs args)
        {
            if (!Context.IsWorldReady)
                return;
            bool voidInInventory = Game1.player.items.Any(item => item?.Name.ToLower().Contains("void") ?? false); ;

            if (voidInInventory == true)
            {
                int voidDecay = 5;
                int decayedHealth = Game1.player.health - (voidDecay / 2);
                float decayedStamina = Game1.player.stamina - voidDecay;
                Game1.player.health = decayedHealth;
                Game1.player.stamina = decayedStamina;

            }
            else
            {
                return;
            }
            


        }

        private void Void_Tolerance(string command, string[] args)
        {
            if (!Context.IsWorldReady)
                return;

            var savedData = this.Helper.ReadJsonFile<SavedData>($"data/{Constants.SaveFolderName}.json") ?? new SavedData();
            this.Monitor.Log($"You have consumed {savedData.Tolerance} void items.");

        }

        private void SpaceEvents_ItemEaten(object sender, EventArgs args) {

            if (!Context.IsWorldReady)
                return;
            //this.Monitor.Log($"{Game1.player.Name} has eaten a {Game1.player.itemToEat.Name}");
            string foodJustEaten = Game1.player.itemToEat.Name;

            if (foodJustEaten.ToLower().Contains("void")){
                //this.Monitor.Log($"{Game1.player.Name} just ate a Void item!");
                Increase_Tolerance();
                Game1.player.stamina = 0;
                hasEatenVoid = true;
            }

        }

        private void Increase_Tolerance() {

            if (!Context.IsWorldReady)
                return;

            var savedData = this.Helper.ReadJsonFile<SavedData>($"data/{Constants.SaveFolderName}.json") ?? new SavedData();
            savedData.Tolerance++;
            Helper.WriteJsonFile($"data/{Constants.SaveFolderName}.json", savedData);
        }

        private void TimeEvents_DayAdvance(object sender, EventArgs args) {
            if (!Context.IsWorldReady)
                return;

            if (hasEatenVoid) {
                Random rnd = new Random();
                int daysToPass = rnd.Next(1, 3);
                Game1.dayOfMonth = (Game1.dayOfMonth + daysToPass);
                hasEatenVoid = false;
            }
        }

   }

    class SavedData {

        public int Tolerance { get; set; }

    }

}