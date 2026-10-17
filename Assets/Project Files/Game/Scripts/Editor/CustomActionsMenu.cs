using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menues customization tutorial
// https://hugecalf-studios.github.io/unity-lessons/lessons/editor/menuitem/#:~:text=To%20disable%20a%20menu%20item,return%20false%20from%20this%20method.

namespace Watermelon
{
    public static class CustomActionsMenu
    {
        #region Resources

        [MenuItem("Actions/Get Resources/None", priority = 23)]
        private static void Reset()
        {
            Currency[] currencies = CurrencyController.Currencies;
            foreach (var currency in currencies)
            {
                CurrencyController.Set(currency.CurrencyType, 0);
            }
        }

        [MenuItem("Actions/Get Resources/All", priority = 24)]
        private static void GetAllResources()
        {
            Currency[] currencies = CurrencyController.Currencies;
            List<Currency> inventoryCurrencies = new List<Currency>();

            foreach (var currency in currencies)
            {
                // food is eaten on pickup, giving it does nothing useful
                if (EnergyController.IsFoorResource(currency.CurrencyType))
                    continue;

                if (currency.Data.UseInventory)
                {
                    CurrencyController.Set(currency.CurrencyType, 0);
                    inventoryCurrencies.Add(currency);
                }
                else
                {
                    CurrencyController.Set(currency.CurrencyType, 10000);
                }
            }

            // bag items share the bag capacity, keep some free space so the player can still pick things up
            PlayerInventory inventory = GetInventory();
            if (inventory != null && inventoryCurrencies.Count > 0)
            {
                int amountPerCurrency = Mathf.FloorToInt(inventory.MaxCapacity * 0.8f / inventoryCurrencies.Count);
                foreach (var currency in inventoryCurrencies)
                {
                    CurrencyController.Set(currency.CurrencyType, amountPerCurrency);
                }
            }
        }

        // Sets the amount, but never more than 80% of the player's bag (a full bag blocks every pickup)
        private static void SetResource(CurrencyType currencyType, int amount)
        {
            Currency currency = CurrencyController.GetCurrency(currencyType);
            PlayerInventory inventory = GetInventory();

            if (currency != null && currency.Data.UseInventory && inventory != null)
            {
                int freeSpace = Mathf.FloorToInt(inventory.MaxCapacity * 0.8f) - (inventory.CurrentCapacity - currency.Amount);
                amount = Mathf.Clamp(amount, 0, Mathf.Max(0, freeSpace));
            }

            CurrencyController.Set(currencyType, amount);
        }

        private static PlayerInventory GetInventory()
        {
            PlayerBehavior player = PlayerBehavior.GetBehavior();

            return player != null ? player.Inventory : null;
        }

        [MenuItem("Actions/Get Resources/Coins", priority = 24)]
        private static void GetCoins()
        {
            CurrencyController.Set(CurrencyType.Coins, 10000);
        }

        [MenuItem("Actions/Get Resources/Wood", priority = 24)]
        private static void GetWood()
        {
            SetResource(CurrencyType.Wood, 2000);
        }

        [MenuItem("Actions/Get Resources/Stone", priority = 25)]
        private static void GetStone()
        {
            SetResource(CurrencyType.Stone, 2000);
        }

        [MenuItem("Actions/Get Resources/Fiber", priority = 26)]
        private static void GetFiber()
        {
            SetResource(CurrencyType.Fiber, 2000);
        }

        [MenuItem("Actions/Get Resources/Rope", priority = 26)]
        private static void GetRope()
        {
            SetResource(CurrencyType.Rope, 2000);
        }

        [MenuItem("Actions/Get Resources/Planks", priority = 26)]
        private static void GetPlanks()
        {
            SetResource(CurrencyType.Planks, 2000);
        }

        [MenuItem("Actions/Get Resources/Bricks", priority = 26)]
        private static void GetBricks()
        {
            SetResource(CurrencyType.Bricks, 2000);
        }

        [MenuItem("Actions/Get Resources/Berries", priority = 26)]
        private static void GetBerries()
        {
            SetResource(CurrencyType.Berries, 2000);
        }

        [MenuItem("Actions/Get Resources/Coconuts", priority = 26)]
        private static void GetCoconuts()
        {
            SetResource(CurrencyType.Coconut, 2000);
        }

        [MenuItem("Actions/Get Resources/Pumpkins", priority = 26)]
        private static void GetPumpkins()
        {
            SetResource(CurrencyType.Pumpkin, 2000);
        }

        [MenuItem("Actions/Get Resources/Fish", priority = 26)]
        private static void GetFish()
        {
            SetResource(CurrencyType.Fish, 2000);
        }

        [MenuItem("Actions/Get Resources/Scissors", priority = 26)]
        private static void GetScissors()
        {
            SetResource(CurrencyType.Scissors, 2000);
        }
        #endregion

        #region Tools

        [MenuItem("Actions/Tools/Unlock Sickle", priority = 26)]
        private static void UnlockSickle()
        {
            UnlockableToolsController.UnlockTool(InteractionAnimationType.Cutting);
        }

        [MenuItem("Actions/Tools/Unlock Fishing Rod", priority = 26)]
        private static void UnlockFishingRod()
        {
            UnlockableToolsController.UnlockTool(InteractionAnimationType.Fishing);
        }

        #endregion

        [MenuItem("Actions/World 1 Scene", priority = 102)]
        private static void World1Scene()
        {
            EditorSceneManager.OpenScene(@"Assets\Project Files\Game\Scenes\Worlds\World 1 [Full Demo].unity");
        }

        [MenuItem("Actions/World 2 Scene", priority = 102)]
        private static void World2Scene()
        {
            EditorSceneManager.OpenScene(@"Assets\Project Files\Game\Scenes\Worlds\World 2 [Alt Demo].unity");
        }
        
        [MenuItem("Actions/Get Resources/None", true)]
        [MenuItem("Actions/Get Resources/All", true)]
        [MenuItem("Actions/Get Resources/Coins", true)]
        [MenuItem("Actions/Get Resources/Wood", true)]
        [MenuItem("Actions/Get Resources/Stone", true)]
        [MenuItem("Actions/Get Resources/Fiber", true)]
        [MenuItem("Actions/Get Resources/Rope", true)]
        [MenuItem("Actions/Get Resources/Berries", true)]
        [MenuItem("Actions/Get Resources/Planks", true)]
        [MenuItem("Actions/Get Resources/Bricks", true)]
        [MenuItem("Actions/Get Resources/Coconuts", true)]
        [MenuItem("Actions/Get Resources/Pumpkins", true)]
        [MenuItem("Actions/Get Resources/Fish", true)]
        [MenuItem("Actions/Get Resources/Scissors", true)]
        private static bool IsGamePlaying()
        {
            return Application.isPlaying;
        }

        [MenuItem("Actions/World 1 Scene", true)]
        [MenuItem("Actions/World 2 Scene", true)]
        private static bool IsGameNotPlaying()
        {
            return !Application.isPlaying;
        }
    }
}