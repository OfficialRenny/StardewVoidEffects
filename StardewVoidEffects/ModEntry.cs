using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoidEffects
{
    public class ModEntry : Mod, IAssetEditor
    {
        private bool hasEatenVoid;
        private bool isMenuOpen;
        private ModConfig Config;
        private int fiveSecondTimer = 5;
        private bool recentlyPassedOutInMP = false;
        private int passedOutMPtimer = 60;

        public override void Entry(IModHelper helper)
        {
            SpaceEvents.OnItemEaten += this.SpaceEvents_ItemEaten;
            TimeEvents.AfterDayStarted += this.TimeEvents_DayAdvance;
            helper.ConsoleCommands.Add("void_tolerance", "Checks how many void items you have consumed.", this.Void_Tolerance);
            GameEvents.OneSecondTick += this.Void_Drain;
            MenuEvents.MenuChanged += this.drainMenu_Open;
            MenuEvents.MenuClosed += this.drainMenu_Closed;
        }

        private void drainMenu_Open(object sender, EventArgsClickableMenuChanged args)
        {
            isMenuOpen = true;
        }

        private void drainMenu_Closed(object sender, EventArgsClickableMenuClosed args)
        {
            isMenuOpen = false;
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals("Data/ObjectInformation"))
            {
                return true;
            }
            else { return false; }
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
            bool voidInInventory = Game1.player.items.Any(item => item?.Name.ToLower().Contains("void") ?? false);

            this.Config = this.Helper.ReadConfig<ModConfig>();

            if (recentlyPassedOutInMP == true && isMenuOpen == false)
            {
                passedOutMPtimer--;
                if (passedOutMPtimer <= 0)
                {
                    recentlyPassedOutInMP = false;
                    passedOutMPtimer = 60;
                }
            }

            if (Game1.player.stamina == 0 && Game1.IsMultiplayer)
            {
                recentlyPassedOutInMP = true;
            }

            if (isMenuOpen == false && recentlyPassedOutInMP == false)
            {
                fiveSecondTimer--;
                if (voidInInventory)
                {
                    if (fiveSecondTimer <= 0 && recentlyPassedOutInMP == false)
                    {
                        int voidDecay = Config.VoidDecay;
                        int decayedHealth = Game1.player.health - (voidDecay / 2);
                        float decayedStamina = Game1.player.stamina - voidDecay;
                        Game1.player.health = decayedHealth;
                        Game1.player.stamina = decayedStamina;
                        fiveSecondTimer = 5;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        private void Void_Tolerance(string command, string[] args)
        {
            if (!Context.IsWorldReady)
                return;

            var savedData = this.Helper.ReadJsonFile<SavedData>($"data/{Constants.SaveFolderName}.json") ?? new SavedData();
            this.Monitor.Log($"You have consumed {savedData.Tolerance} void items.");
        }

        private void SpaceEvents_ItemEaten(object sender, EventArgs args)
        {
            if (!Context.IsWorldReady)
                return;

            //this.Monitor.Log($"{Game1.player.Name} has eaten a {Game1.player.itemToEat.Name}");
            string foodJustEaten = Game1.player.itemToEat.Name;

            if (foodJustEaten.ToLower().Contains("void"))
            {
                //this.Monitor.Log($"{Game1.player.Name} just ate a Void item!");
                if (Context.IsMultiplayer)
                {
                    Increase_Tolerance();
                    if (Game1.player.stamina > 10)
                    {
                        Game1.player.stamina = 10;
                    }
                    hasEatenVoid = false;
                }
                else
                {
                    Increase_Tolerance();
                    Game1.player.stamina = 0;
                    hasEatenVoid = true;
                }
            }
        }

        private void Increase_Tolerance()
        {
            if (!Context.IsWorldReady)
                return;

            var savedData = this.Helper.ReadJsonFile<SavedData>($"data/{Constants.SaveFolderName}.json") ?? new SavedData();
            savedData.Tolerance++;
            Helper.WriteJsonFile($"data/{Constants.SaveFolderName}.json", savedData);
        }

        private void TimeEvents_DayAdvance(object sender, EventArgs args)
        {
            int noOfPlayers = Game1.getOnlineFarmers().Count<Farmer>();
            if (!Context.IsWorldReady)
            {
                return;
            }

            if (hasEatenVoid && !Game1.IsMultiplayer)
            {
                this.Monitor.Log($"There are currently {noOfPlayers} players in game. (The number should be 1, if not send halp)");
                Random rnd = new Random();
                int daysToPass = rnd.Next(1, 4);
                Game1.dayOfMonth = (Game1.dayOfMonth + daysToPass);
                hasEatenVoid = false;
            }
            else
            {
                this.Monitor.Log($"There are currently {noOfPlayers} number of players in your game. \nIf you receive this and you're in singleplayer, something went wrong.");
                hasEatenVoid = false;
            }
        }
    }

    internal class SavedData
    {
        public int Tolerance { get; set; }
    }

    internal class ModConfig
    {
        public float VoidItemPriceIncrease { get; set; } = 2.0f;
        public int VoidDecay { get; set; } = 10;
    }
}