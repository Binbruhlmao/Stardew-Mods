﻿using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using MultiplayerMod.Framework.Patch;
using MultiplayerMod.Framework;
using System.Reflection;
using System;
using MultiplayerMod.Framework.Command;
using StardewValley.Menus;
using MultiplayerMod.Framework.Mobile.Menus;
using StardewValley.Monsters;
using System.IO;
using StardewValley;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Network;
using MultiplayerMod.Framework.Network;

namespace MultiplayerMod
{
    public class ModEntry : Mod
    {
        internal Config config;
        internal PatchManager PatchManager { get; set; }
        internal static IModHelper ModHelper;
        internal static IMonitor ModMonitor;
        internal CommandManager CommandManager { get; set; }
        internal PropertyInfo tapToMoveProperty;
        public override void Entry(IModHelper helper)
        {

            ModUtilities.Helper = helper;
            ModUtilities.ModMonitor = Monitor;
            config = helper.ReadConfig<Config>();
            ModUtilities.ModConfig = config;
            ModHelper = helper;
            ModMonitor = Monitor;
            PatchManager = new PatchManager(Helper, ModManifest, config);
            PatchManager.Apply();
            CommandManager = new CommandManager();
            CommandManager.Apply(helper);
            //Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            if (ModUtilities.IsAndroid)
            {
                tapToMoveProperty = typeof(GameLocation).GetProperty("tapToMove");
                Helper.Events.GameLoop.UpdateTicking += OnUpdating;
            }
        }



        void OnUpdating(object sender, UpdateTickingEventArgs e)
        {
            if (Game1.currentLocation != null && Game1.gameMode == Game1.playingGameMode && Game1.client != null)
            {
                if (tapToMoveProperty.GetValue(Game1.currentLocation) == null)
                {
                    object TapToMove = typeof(IClickableMenu).Assembly.GetType("StardewValley.Mobile.TapToMove").CreateInstance<object>(new object[] { Game1.currentLocation });
                    tapToMoveProperty.SetValue(Game1.currentLocation, TapToMove);
                }
            }
        }


        void OnSaveLoaded(object sender, SaveLoadedEventArgs eventArgs)
        {
            if (Game1.player.slotCanHost)
            {
                if (Game1.server != null)
                {
                    List<Server> servers = ModUtilities.Helper.Reflection.GetField<List<Server>>(Game1.server, "servers").GetValue();
                    foreach (Server server in servers)
                    {
                        if (server != null)
                        {
                            if (!server.connected())
                            {
                                server.initialize();
                            }
                        }
                    }
                }
                else
                {
                    Game1.multiplayerMode = 2;
                    Game1.server = new GameServer(false);
                    Game1.server.initializeHost();
                }
            }

        }

        void OnMenuChanged(object sender, MenuChangedEventArgs eventArgs)
        {
            if (eventArgs.NewMenu != null)
            {
                if (eventArgs.NewMenu is TitleMenu)
                {
                    Game1.client?.disconnect(false);
                    Game1.server?.stopServer();
                }
            }
        }

        void OnPlayerWarped(object sender, StardewModdingAPI.Events.WarpedEventArgs warpedEventArgs)
        {
            if (warpedEventArgs.NewLocation != null && ModUtilities.IsAndroid)
            {
                var property = warpedEventArgs.NewLocation.GetType().GetProperty("tapToMove");
                if (property.GetValue(warpedEventArgs.NewLocation) == null)
                {
                    object TapToMove = typeof(IClickableMenu).Assembly.GetType("StardewValley.Mobile.TapToMove").CreateInstance<object>(new object[] { warpedEventArgs.NewLocation });
                    property.SetValue(warpedEventArgs.NewLocation, TapToMove);
                }
#if DEBUG
                Monitor.Log("New Location " + warpedEventArgs.NewLocation.name, LogLevel.Warn);
#endif
            }
        }

        /// <summary>
        /// This is for debugging, never mind it ...
        /// </summary>
        void ApplyDebug()
        {
#if DEBUG

            var listField = new List<IReflectedField<object>>();
            /*
            try
            {
                foreach(var field in typeof(Game1).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    try
                    {
                        listField.Add(Helper.Reflection.GetField<object>(Game1.game1, field.Name));
                    }
                    catch(Exception ex)
                    {
                        Monitor.Log(ex.GetBaseException().ToString() , LogLevel.Error);
                    }
                }
                foreach (var field in typeof(Game1).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    try
                    {
                        listField.Add(Helper.Reflection.GetField<object>(Game1.game1.GetType(), field.Name));
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log(ex.GetBaseException().ToString(), LogLevel.Error);
                    }
                }
            }
            catch(Exception ex)
            {

            }*/
            Helper.Events.Input.ButtonPressed += (o, e) =>
            {
                if (e.Button == SButton.M)
                {
                    CommandManager.GetCommand("client_connectMenu").Execute("client_connectMenu", new string[] { });
                }
                else if (e.Button == SButton.N)
                {
                    Game1.activeClickableMenu = new SCoopMenuMobile();
                }
                else if (e.Button == SButton.P)
                {
                    Game1.activeClickableMenu = new CharacterCustomization(CharacterCustomization.Source.HostNewFarm);
                }
                else if (e.Button == SButton.L)
                {
                    Game1.activeClickableMenu = new SCoopGameMenu(false);
                }
            };
            /*Helper.Events.GameLoop.UpdateTicked += (o, e) =>
            {
                if (Game1.currentLocation != null && e.Ticks % 20 == 0)
                {
                    if(ModUtilities.IsAndroid)
                    Monitor.Log($"GameMode {Game1.gameMode}", LogLevel.Debug);
                    try
                    {
                        Monitor.Log("Starting Field Game1 Debug" , LogLevel.Alert);
                        foreach (var field in listField)
                        {
                            Monitor.Log($"Field  {field.FieldInfo.Name} Is Null ({field.GetValue() != null}) | TYPE {field.FieldInfo.FieldType.Namespace + "." + field.FieldInfo.FieldType.Name}", LogLevel.Debug);
                        }
                        Monitor.Log("Stopped Field Game1 Debug", LogLevel.Alert);
                    }
                    catch(Exception ex)
                    {

                    }
                }
            };*/
#endif
        }

    }
}