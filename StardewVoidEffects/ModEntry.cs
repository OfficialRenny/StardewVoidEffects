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
using System.Collections.Generic;

namespace StardewVoidEffects
{

    public class ModEntry : Mod, IAssetEditor
    {
        bool hasEatenVoid;
        private ModConfig Config;


        public override void Entry(IModHelper helper)
        {

            SpaceEvents.OnItemEaten += this.SpaceEvents_ItemEaten;
            TimeEvents.AfterDayStarted += this.TimeEvents_DayAdvance;
            helper.ConsoleCommands.Add("void_tolerance", "Checks how many void items you have consumed.", this.Void_Tolerance);
            GameEvents.OneSecondTick += this.Void_Drain;
            //GameEvents.FirstUpdateTick += this.Check_For_Mods;
        }
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals("Data/ObjectInformation"))
            {
                return true;
            } else { return false; }
        }
        public void Edit<T>(IAssetData asset)
        {
            int[] validItems = { 305, 308, 174, 176, 180, 182, 184, 186, 442, 436, 438, 440, 444, 446, 306, 307, 424, 426, 428, 769, 795 };
            int[] validItemsVanilla = { 305, 308, 769, 795 };
            bool isVoidRanchLoaded = this.Helper.ModRegistry.IsLoaded("Taelende.VoidRanch");
            this.Config = this.Helper.ReadConfig<ModConfig>();
            float priceIncrease = this.Config.VoidItemPriceIncrease;

            if (asset.AssetNameEquals("Data/ObjectInformation"))
            {
                if (isVoidRanchLoaded)
                {
                    IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                    foreach (int id in validItems)
                    {
                        if (data.TryGetValue(id, out string entry))
                        {
                            string[] fields = entry.Split('/');
                            int currentPrice = int.Parse(fields[1]);
                            fields[1] = (currentPrice * priceIncrease).ToString();
                            data[id] = string.Join("/", fields);
                        }
                    }
                }
                else
                {
                    IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                    foreach (int id in validItemsVanilla)
                    {
                        if (data.TryGetValue(id, out string entry))
                        {
                            string[] fields = entry.Split('/');
                            int currentPrice = int.Parse(fields[1]);
                            fields[1] = (currentPrice * priceIncrease).ToString();
                            data[id] = string.Join("/", fields);
                        }
                    }
                }
                
            }
        }

        private void Void_Drain(object sender, EventArgs args)
        {
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
            if (hasEatenVoid) {
                Random rnd = new Random();
                int daysToPass = rnd.Next(1, 4);
                Game1.dayOfMonth = (Game1.dayOfMonth + daysToPass);
                hasEatenVoid = false;
            }
        }

   }

    class SavedData {

        public int Tolerance { get; set; }

    }

    class ModConfig
    {
        public float VoidItemPriceIncrease { get; set; } = 2.0f;
    }

}