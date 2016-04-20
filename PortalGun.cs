/*
 * Portal Gun
 * Author: Pandassaurus
 * Version: 2.2
 * Donate if you enjoy!
 * If you're reading this, tell me! I'm curious to know how many people like having access to the code and enjoy the comments!
 */


using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

public class PortalGun : Script
{
    private Prop bluePortal;
    private Ped blueTestPed;
    private bool blueTeleportedRecently;
    private bool bluePortalInspace;
    private Prop orangePortal;
    private Ped orangeTestPed;
    private bool orangeTeleportedRecently;
    private bool orangePortalInSpace;

    private List<Ped> orangeTPRecentlyPeds = new List<Ped>();
    List<Vehicle> orangeTpRecentlyVehicles = new List<Vehicle>();
    private List<Ped> blueTPRecentlyPeds = new List<Ped>();
    List<Vehicle> blueTpRecentlyVehicles = new List<Vehicle>();

    private Keys keyholdForBluePortal = Keys.T;
    private Keys keyGrabEntity = Keys.E;
    private bool toggleHoldForBluePortal = true;
    private Keys keyToggleLongFall = Keys.O;
    private bool toggleCrosshairsWhileFalling = true;
    private Keys keyTpBluePortal = Keys.K;
    private Keys keyTpOrangePortal = Keys.L;
    private Keys keySpawnPed = Keys.B;
    private Keys keyToggleSuckInMoon = Keys.I;
    private Keys keyPlacePortalOnMoon = Keys.Y;
    private int moonSuckDistance = 15;

    private bool allSoundsLoaded = true;
    SoundPlayer playerInvalid;
    SoundPlayer playerPortalOpen1;
    SoundPlayer playerPortalOpen2;
    SoundPlayer playerPowerup;
    SoundPlayer playerShootOrange;
    SoundPlayer playerShootBlue;
    SoundPlayer playerSpace;

    private bool allTexturesLoaded = true;
    private String textureNoShot;
    private String texturePortalBlue;
    private String texturePortalBoth;
    private String texturePortalNeither;
    private String texturePortalOrange;
    private String textureShot;


    private List<Ped> pedsBeingFlung = new List<Ped>();
    private List<Vehicle> vehiclesBeingFlung = new List<Vehicle>();

    private Entity attachedEntity;
    private bool isEntityAttached;
    private bool longFallBootsOn;
    private bool canMoonSuckIn;
    private bool hasPlugged = false;
    private String version = "v2.2";
    private bool isTPressed = false;

    public PortalGun()
    {
        Tick += OnTick;
        KeyUp += OnKeyUp;
        KeyDown += OnKeyDown;
        GetBindings();
        EnableSound();
        GetTextures();
    }

    #region Init stuff
    private void GetBindings()
    {
        //create variable, then try to read text of file
        string getSettings;
        try
        {
            getSettings = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\PortalGunSettings.txt");
        }
        catch
        {
            //if not, ignore key bindings, use defualts
            UI.Notify("PortalGunSettings.txt is either missing or altered. Custom key bindings will be ignored.");
            return;
        }

        //There are better ways of doing it, but I'm too lazy to do those other ways
        string[] enteredValues = getSettings.Split('=');

        string[] bindValue1 = enteredValues[1].Split('\n');
        keyholdForBluePortal = (Keys)Enum.Parse(typeof(Keys), bindValue1[0]);

        string[] bindValue2 = enteredValues[2].Split('\n');
        keyGrabEntity = (Keys)Enum.Parse(typeof(Keys), bindValue2[0]);

        string[] bindValue3 = enteredValues[3].Split('\n');
        keyToggleLongFall = (Keys)Enum.Parse(typeof(Keys), bindValue3[0]);

        string[] bindValue4 = enteredValues[4].Split('\n');
        keyTpBluePortal = (Keys)Enum.Parse(typeof(Keys), bindValue4[0]);

        string[] bindValue5 = enteredValues[5].Split('\n');
        keyTpOrangePortal = (Keys)Enum.Parse(typeof(Keys), bindValue5[0]);

        string[] bindValue6 = enteredValues[6].Split('\n');
        keySpawnPed = (Keys)Enum.Parse(typeof(Keys), bindValue6[0]);

        string[] bindValue7 = enteredValues[7].Split('\n');
        keyToggleSuckInMoon = (Keys)Enum.Parse(typeof(Keys), bindValue7[0]);

        string[] bindValue8 = enteredValues[8].Split('\n');
        keyPlacePortalOnMoon = (Keys)Enum.Parse(typeof(Keys), bindValue8[0]);

        string[] bindValue9 = enteredValues[9].Split('\n');
        toggleHoldForBluePortal = Convert.ToBoolean(bindValue9[0]);

        string[] bindValue10 = enteredValues[10].Split('\n');
        toggleCrosshairsWhileFalling = Convert.ToBoolean(bindValue10[0]);

        string[] bindValue11 = enteredValues[11].Split('\n');
        moonSuckDistance = Convert.ToInt32(bindValue11[0]);
    }
    private void EnableSound()
    {
        //same pattern - create string with location, see if it exists, then load if it does. if not, no sounds at all
        String soundInvalid = (AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portal_invalid_surface.wav");
        if (!File.Exists(soundInvalid)) allSoundsLoaded = false;
        else
        {
            playerInvalid = new SoundPlayer(soundInvalid);
            playerInvalid.Load();
        }
        String soundPortalOpen1 = (AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portal_open1.wav");
        if (!File.Exists(soundPortalOpen1)) allSoundsLoaded = false;
        else
        {
            playerPortalOpen1 = new SoundPlayer(soundPortalOpen1);
            playerPortalOpen1.Load();
        }
        String soundPortalOpen2 = (AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portal_open2.wav");
        if (!File.Exists(soundPortalOpen2)) allSoundsLoaded = false;
        else
        {
            playerPortalOpen2 = new SoundPlayer(soundPortalOpen2);
            playerPortalOpen2.Load();
        }
        String soundPowerup = (AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portalgun_powerup.wav");
        if (!File.Exists(soundPowerup)) allSoundsLoaded = false;
        else
        {
            playerPowerup = new SoundPlayer(soundPowerup);
            playerPowerup.Load();
        }
        String soundShootOrange = (AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portalgun_shoot_red.wav");
        if (!File.Exists(soundShootOrange)) allSoundsLoaded = false;
        else
        {
            playerShootOrange = new SoundPlayer(soundShootOrange);
            playerShootOrange.Load();
        }
        String soundShootBlue = (AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portalgun_shoot_blue.wav");
        if (!File.Exists(soundShootBlue)) allSoundsLoaded = false;
        else
        {
            playerShootBlue = new SoundPlayer(@soundShootBlue);
            playerShootBlue.Load();
        }
        String soundSpace = (AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\space.wav");
        if (!File.Exists(soundSpace)) allSoundsLoaded = false;
        else
        {
            playerSpace = new SoundPlayer(@soundSpace);
            playerSpace.Load();
        }
        if (!allSoundsLoaded)
        {
            UI.Notify("Some sounds are missing or renamed. Custom sounds will be disabled.");
            return;
        }
        allSoundsLoaded = true;
    }
    private void GetTextures()
    {
        //create String, see if it exists, then continue
        textureNoShot = AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\noShot.png";
        if (!File.Exists(textureNoShot)) allTexturesLoaded = false;
        texturePortalBlue = AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portalBlue.png";
        if (!File.Exists(texturePortalBlue)) allTexturesLoaded = false;
        texturePortalBoth = AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portalBoth.png";
        if (!File.Exists(texturePortalBoth)) allTexturesLoaded = false;
        texturePortalNeither = AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portalNeither.png";
        if (!File.Exists(texturePortalNeither)) allTexturesLoaded = false;
        texturePortalOrange = AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\portalOrange.png";
        if (!File.Exists(texturePortalOrange)) allTexturesLoaded = false;
        textureShot = AppDomain.CurrentDomain.BaseDirectory + "\\PortalResources\\shot.png";
        if (!File.Exists(textureShot)) allTexturesLoaded = false;

        if (!allTexturesLoaded)
        {
            UI.Notify("Some textures are missing or renamed. Custom textures will be disabled.");
            return;
        }
        allTexturesLoaded = true;
    }

    #endregion

    #region Standard
    private void OnTick(object sender, EventArgs e)
    {
        //Heart of the mod...
        AddPortals(false, false);
        CheckPlayerTeleporting();
        CheckEntityTeleporting();
        RefreshPlayerTimeouts();
        RefreshEntityTimeouts();
        SuckInBecauseOfMoon();
        DrawCrosshairTextures();
        Maintenance();
    }
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        var playerPed = Game.Player.Character;

        //TeleportPed directly to orange tpToPortal
        if (e.KeyCode == keyTpOrangePortal && keyTpOrangePortal != Keys.None && orangePortal != null)
        {
            playSound(playerPortalOpen2);
            playerPed.Position = orangePortal.Position + new Vector3(0, 0, 1);
            blueTeleportedRecently = true;
        }

        //TeleportPed directly to blue tpToPortal
        if (e.KeyCode == keyTpBluePortal && keyTpBluePortal != Keys.None && bluePortal != null)
        {
            playSound(playerPortalOpen2);
            playerPed.Position = bluePortal.Position + new Vector3(0, 0, 1f);
            orangeTeleportedRecently = true;
        }

        //Long fall boots
        if (e.KeyCode == keyToggleLongFall && keyToggleLongFall != Keys.None)
        {
            playSound(playerPowerup);
            if (longFallBootsOn)
            {
                //set all kinds of damage on
                Function.Call(Hash.SET_ENTITY_PROOFS, playerPed, false, false, false, false, false, false, false, false);
                UI.ShowSubtitle("Long Fall Boots Off");
                longFallBootsOn = false;
            }
            else
            {
                //set collide damage off
                Function.Call(Hash.SET_ENTITY_PROOFS, playerPed, false, false, false, true, false, false, false, false);
                UI.ShowSubtitle("Long Fall Boots On");
                longFallBootsOn = true;
            }
        }

        if (e.KeyCode == keySpawnPed && keySpawnPed != Keys.None)
        {
            //create ped
            playSound(playerPortalOpen1);
            World.CreateRandomPed(playerPed.Position + playerPed.ForwardVector * 2);
        }
        if (e.KeyCode == keyToggleSuckInMoon && keyToggleSuckInMoon != Keys.None)
        {
            playSound(playerPowerup);
            canMoonSuckIn = !canMoonSuckIn;
            UI.ShowSubtitle("Player can be sucked in by moon: " + canMoonSuckIn);
        }

        //used to see if the key is being held down for shooting blue portal
        if (e.KeyCode == keyholdForBluePortal)
        {
            if (!toggleHoldForBluePortal)
            {
                AddPortals(true, false);
                return;
            }
            isTPressed = false;
        }

        if (e.KeyCode == keyPlacePortalOnMoon) AddPortals(false, true);

        if (e.KeyCode == keyGrabEntity) GrabEntityWithPortalGun();
    }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        //used to see if the key is being held down for shooting blue portal
        if (e.KeyCode == keyholdForBluePortal)
        {
            isTPressed = true;
        }
    }

    #endregion
    
    #region Big Boy
    private void AddPortals(bool isOveridden, bool directToMoon)
    {
        var playerPed = Game.Player.Character;
        if (isEntityAttached) return;
        if (playerPed.IsShooting || (isOveridden && Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING, Game.Player)) || directToMoon)
        {
            //Get the Coords of the impact by creating a raycast, then getting the hit coords of that. alot of calculations here that i have not clue what they do
            Vector3 result = Vector3.Zero;
            RaycastResult ray = GetRayFromCam();
            result = ray.HitCoords;
            if (ray.HitEntity == playerPed) result = Vector3.Zero;

            var weaponHash = playerPed.Weapons.Current.Hash.GetHashCode();

            //if player shot moon
            if ((IsPlayerLookingAtMoon() && !ray.DitHitAnything) || directToMoon)
            {
                if (weaponHash == WeaponHash.HeavyPistol.GetHashCode() && (!isTPressed && toggleHoldForBluePortal))
                {
                    result = new Vector3(4, 4, 2500); //2500
                    orangePortalInSpace = true;

                    //plug and play ... sound
                    Plug();
                    playSound(playerShootOrange);

                    //make sure it exists
                    if (orangePortal != null) orangePortal.Delete();

                    //create model, make it mostly invincible
                    //prop_bskball_01, v_ilev_exball_grey, prop_target_orange_arrow (possible models, went with ex ball because of size)
                    orangePortal = World.CreateProp(new Model("v_ilev_exball_grey"), result, false, false);
                    Function.Call(Hash.SET_ENTITY_PROOFS, orangePortal, true, true, true, true, true, true, true, true);
                    //freeze position, blip stuff
                    orangePortal.FreezePosition = true;
                    orangePortal.AddBlip();
                    orangePortal.CurrentBlip.Color = BlipColor.Yellow;
                }

                //blue tpToPortal
                else if (weaponHash == WeaponHash.HeavyPistol.GetHashCode() && ((isTPressed && toggleHoldForBluePortal) || isOveridden))
                {
                    result = new Vector3(-4, -4, 2500); //2500
                    bluePortalInspace = true;

                    //plug and play ... sound
                    Plug();
                    playSound(playerShootBlue);

                    //make sure it exists
                    if (bluePortal != null) bluePortal.Delete();

                    //create model, make it mostly invincible
                    //prop_bowling_ball, prop_swiss_ball_01, prop_target_blue_arrow (possible models, went with ex ball because of size)
                    bluePortal = World.CreateProp(new Model("prop_swiss_ball_01"), result, false, false);
                    Function.Call(Hash.SET_ENTITY_PROOFS, bluePortal, true, true, true, true, true, true, true, true);
                    //freeze position, blip stuff
                    bluePortal.FreezePosition = true;
                    bluePortal.AddBlip();
                    bluePortal.CurrentBlip.Color = BlipColor.Blue;
                }
            }

            //Make sure they exist
            else if (result != Vector3.Zero && ray.DitHitAnything && result != Vector3.Zero.Around(10))
            {
                //get list of vehicles in the hitcoords so if theres a vehicle/ped there, it can be directly teleported
                List<Vehicle> vehs = new List<Vehicle>(World.GetNearbyVehicles(result, 2.5f));
                List<Ped> peds = new List<Ped>(World.GetNearbyPeds(result, .5f));

                //orange tpToPortal
                if (weaponHash == WeaponHash.HeavyPistol.GetHashCode() && (!isTPressed && toggleHoldForBluePortal))
                {
                    //plug and play ... sound
                    Plug();
                    playSound(playerShootOrange);
                    orangePortalInSpace = false;

                    //teleport vehicles/peds in list
                    foreach (Vehicle veh in vehs) if (orangePortal != null && bluePortal != null) TeleportVehicle(veh, bluePortal);
                    foreach (Ped ped in peds) if (orangePortal != null && bluePortal != null) TeleportPed(ped, bluePortal);

                    //make sure it exists
                    if (orangePortal != null) orangePortal.Delete();

                    //create model, make it mostly invincible
                    //prop_bskball_01, v_ilev_exball_grey, prop_target_orange_arrow (possible models, went with ex ball because of size)
                    orangePortal = World.CreateProp(new Model("v_ilev_exball_grey"), result, false, false);
                    Function.Call(Hash.SET_ENTITY_PROOFS, orangePortal, true, true, true, true, true, true, true, true);
                    //freeze position, blip stuff
                    orangePortal.FreezePosition = true;
                    orangePortal.AddBlip();
                    orangePortal.CurrentBlip.Color = BlipColor.Yellow;
                }

                //blue tpToPortal
                else if (weaponHash == WeaponHash.HeavyPistol.GetHashCode() && ((isTPressed && toggleHoldForBluePortal) || isOveridden))
                {
                    //plug and play ... sound
                    Plug();
                    playSound(playerShootBlue);
                    bluePortalInspace = false;

                    //teleport vehicles/peds in list
                    foreach (Vehicle veh in vehs) if (orangePortal != null && bluePortal != null) TeleportVehicle(veh, orangePortal);
                    foreach (Ped ped in peds) if (orangePortal != null && bluePortal != null) TeleportPed(ped, orangePortal);

                    //make sure it exists
                    if (bluePortal != null) bluePortal.Delete();

                    //create model, make it mostly invincible
                    //prop_bowling_ball, prop_swiss_ball_01, prop_target_blue_arrow (possible models, went with ex ball because of size)
                    bluePortal = World.CreateProp(new Model("prop_swiss_ball_01"), result, false, false);
                    Function.Call(Hash.SET_ENTITY_PROOFS, bluePortal, true, true, true, true, true, true, true, true);
                    //freeze position, blip stuff
                    bluePortal.FreezePosition = true;
                    bluePortal.AddBlip();
                    bluePortal.CurrentBlip.Color = BlipColor.Blue;
                }
            }
            else
            {
                if (playerPed.Weapons.Current.Hash.GetHashCode() == WeaponHash.HeavyPistol.GetHashCode())
                {
                    //play sound letting the player know it has not been created
                    playSound(playerInvalid);
                }
            }
        }
    }
    private void RefreshPortals()
    {
        //in case they pop...
        if (orangePortal != null)
        {
            //save previous location before deleting
            Vector3 opLocation = orangePortal.Position - new Vector3(0, 0, .45f);
            orangePortal.Delete();
            //create in same location, add blip stuff
            orangePortal = World.CreateProp(new Model("v_ilev_exball_grey"), opLocation, false, false);
            Function.Call(Hash.SET_ENTITY_PROOFS, orangePortal, true, true, true, true, true, true, true, true);
            orangePortal.FreezePosition = true;
            orangePortal.AddBlip();
            orangePortal.CurrentBlip.Color = BlipColor.Yellow;
        }
        if (bluePortal != null)
        {
            //save previous location before deleting
            Vector3 bpLocation = bluePortal.Position - new Vector3(0, 0, .45f);
            bluePortal.Delete();
            //create in same location, add blip stuff
            bluePortal = World.CreateProp(new Model("prop_swiss_ball_01"), bpLocation, false, false);
            Function.Call(Hash.SET_ENTITY_PROOFS, bluePortal, true, true, true, true, true, true, true, true);
            bluePortal.FreezePosition = true;
            bluePortal.AddBlip();
            bluePortal.CurrentBlip.Color = BlipColor.Blue;
        }
    }
    private void Maintenance()
    {
        Ped playerPed = Game.Player.Character;

        //gives player infinite ammo
        if (WeaponHash.HeavyPistol.GetHashCode() == playerPed.Weapons.Current.Hash.GetHashCode())
            playerPed.Weapons.Current.InfiniteAmmoClip = true;
        else playerPed.Weapons.Current.InfiniteAmmoClip = false;

        //will make sure they don't pop
        if (orangePortal != null || bluePortal != null)
        {
            Ped testPed = World.CreatePed(PedHash.Crow, Vector3.Zero);
            testPed.IsVisible = false;
            testPed.FreezePosition = true;
            testPed.IsInvincible = true;
            if (orangePortal != null)
            {
                testPed.Position = orangePortal.Position;
                if (!testPed.IsTouching(orangePortal)) RefreshPortals();
            }
            if (bluePortal != null)
            {
                testPed.Position = bluePortal.Position;
                if (!testPed.IsTouching(bluePortal)) RefreshPortals();
            }
            testPed.Delete();
        }

        if (isEntityAttached && (!Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING, Game.Player) || playerPed.IsRagdoll || playerPed.IsProne) && attachedEntity != null) LetGoOfGrabbedEntity();
    }
    private void DrawCrosshairTextures()
    {
        //make sure all textures are loaded
        if (allTexturesLoaded)
        {
            Ped playerPed = Game.Player.Character;

            //some qualifiers
            if (WeaponHash.HeavyPistol.GetHashCode() == playerPed.Weapons.Current.Hash.GetHashCode() &&
                (Function.Call<bool>(Hash.IS_AIM_CAM_ACTIVE) || Function.Call<bool>(Hash.IS_FIRST_PERSON_AIM_CAM_ACTIVE) || (playerPed.IsRagdoll && toggleCrosshairsWhileFalling)))
            {
                //get the res, then get a pos from that using a hardcoded function
                Point pos = new Point(618, 344);

                //see if the portals have been placed
                String finalText;
                if (bluePortal == null && orangePortal == null) finalText = texturePortalNeither;
                else if (bluePortal != null && orangePortal == null) finalText = texturePortalBlue;
                else if (bluePortal == null && orangePortal != null) finalText = texturePortalOrange;
                else if (bluePortal != null && orangePortal != null) finalText = texturePortalBoth;
                else return;
                UI.DrawTexture(finalText, 1, 0, 100, pos, new Size(45, 35));

                //get the raycast to change the crosshair color
                RaycastResult ray = GetRayFromCam();
                if ((ray.DitHitAnything && ray.HitCoords != Vector3.Zero.Around(10) && ray.HitEntity != playerPed) || IsPlayerLookingAtMoon()) finalText = textureShot;
                else finalText = textureNoShot;
                UI.DrawTexture(finalText, 1, 0, 100, new Point(pos.X - 5, pos.Y - 2), new Size(56, 37));
            }
        }
    }

    #endregion

    #region Teleporting
    void CheckPlayerTeleporting()
    {
        Ped playerPed = Game.Player.Character;
        //make sure they exist
        if (bluePortal != null && orangePortal != null)
        {
            //not bother checking if far
            if (playerPed.IsNearEntity(orangePortal, new Vector3(5, 5, 5)) ||
                playerPed.IsNearEntity(bluePortal, new Vector3(5, 5, 5)))
            {
                //if touching and not teleported recently, TeleportPed
                if (orangePortal.IsTouching(playerPed) && !orangeTeleportedRecently)
                {
                    TeleportPed(playerPed, bluePortal);
                    orangeTeleportedRecently = true;
                }
                //if touching and not teleported recently, TeleportPed
                else if (bluePortal.IsTouching(playerPed) && !blueTeleportedRecently)
                {
                    TeleportPed(playerPed, orangePortal);
                    blueTeleportedRecently = true;
                }
            }
        }
    }
    private void CheckEntityTeleporting()
    {
        //Entity Teleporting
        if (orangePortal != null && bluePortal != null) 
        {
            //get list of peds/vehicles near each portal
            Tuple<Ped[], Vehicle[]> pedsAndVehsNearOrange = GetNearestPedsAndVehiclestoPortal(true, 5, Vector3.Zero);
            Tuple<Ped[], Vehicle[]> pedsAndVehsNearBlue = GetNearestPedsAndVehiclestoPortal(false, 5, Vector3.Zero);
            List<Ped> pedsNearOrange = new List<Ped>(pedsAndVehsNearOrange.Item1);
            List<Vehicle> vehiclesNearOrange = new List<Vehicle>(pedsAndVehsNearOrange.Item2);
            List<Ped> pedsNearBlue = new List<Ped>(pedsAndVehsNearBlue.Item1);
            List<Vehicle> vehiclesNearBlue = new List<Vehicle>(pedsAndVehsNearBlue.Item2);

            //check them all if they're touching and if they're not on timeout, then send off
            foreach (Ped po in pedsNearOrange)
            {
                if (po.IsTouching(orangePortal) && !IsPedOnTimeout(po, false))
                {
                    TeleportPed(po, bluePortal);
                    orangeTPRecentlyPeds.Add(po);
                }
            }
            foreach (Vehicle vo in vehiclesNearOrange)
            {
                if (vo.IsTouching(orangePortal) && !IsVehicleOnTimeout(vo, false))
                {
                    TeleportVehicle(vo, bluePortal);
                    orangeTpRecentlyVehicles.Add(vo);
                }
            }
            foreach (Ped pb in pedsNearBlue)
            {
                if (pb.IsTouching(bluePortal) && !IsPedOnTimeout(pb, true))
                {
                    TeleportPed(pb, orangePortal);
                    blueTPRecentlyPeds.Add(pb);
                }
            }
            foreach (Vehicle vb in vehiclesNearBlue)
            {
                if(vb.IsTouching(bluePortal) && !IsVehicleOnTimeout(vb, true))
                {
                    TeleportVehicle(vb, orangePortal);
                    blueTpRecentlyVehicles.Add(vb);
                }
            }
        }
    }
    private void TeleportPed(Ped ped, Prop tpToPortal)
    {
        //make sure you're not checking the teleportPed crow
        if (ped.Model.ToString() == "0x18012A9F") return;

        //if in a vehicle, redirect to teleport vehicle
        if (ped.IsInVehicle())
        {
            TeleportVehicle(ped.CurrentVehicle, tpToPortal);
            return;
        }

        //get force of how fast player was going/direction and create offset for when the player spawns
        var force = ped.Velocity * 1.65f;
        force.Z = MakePositive(force.Z);
        if(force == Vector3.Zero) force = new Vector3(0, 0, -1);
        Vector3 tpOffset = new Vector3(0, 0, .9f);

        //if teleporting to space, shoot out bottom and play space sound
        if ((tpToPortal.Position == orangePortal.Position && orangePortalInSpace) || (tpToPortal.Position == bluePortal.Position && bluePortalInspace))
        {
            force.Z *= -20;
            tpOffset = new Vector3(0, 0, -3.5f);
            if(ped == Game.Player.Character) playSound(playerSpace);
        }

        //check ragdoll for continuity, set position
        var wasRagdoll = ped.IsRagdoll;
        ped.Position = tpToPortal.Position + tpOffset;
        if (wasRagdoll) Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, 200, 200, 0, false, false, false);
        //apply force, set tpToPortal use reset
        ped.ApplyForce(force);
        RefreshPortals();
    }
    void TeleportVehicle(Vehicle veh, Prop tpToPortal)
    {
        //get the velocity, and make sure it's not a bad direction
        Vector3 force = veh.Velocity;
        force.Z = MakePositive(force.Z);
        if(force == Vector3.Zero) force = veh.ForwardVector * 2;
        Vector3 tpOffset = new Vector3(0, 0, 1);

        //if teleporting to space, shoot out bottom and play space sound
        if (((tpToPortal.Position == orangePortal.Position && orangePortalInSpace) ||
             (tpToPortal.Position == bluePortal.Position && bluePortalInspace)) &&
            veh.GetPedOnSeat(VehicleSeat.Driver) == Game.Player.Character)
        {
            tpOffset = new Vector3(0, 0, -3.5f) - new Vector3(0, 0, veh.Model.GetDimensions().Y);
            playSound(playerSpace);
        }

        //set position, including distance from portal so you don't fall right back in
        veh.Position = tpToPortal.Position + tpOffset + veh.ForwardVector * veh.Model.GetDimensions().Y;
        veh.ApplyForce(force);
        RefreshPortals();
    }
    Tuple<Ped[], Vehicle[]> GetNearestPedsAndVehiclestoPortal(bool isOrange, int distance, Vector3 overrideOffset)
    {
        //i return both at once so when i check rapidly for the moon sucking, i only have to move the testped once at a time
        if (isOrange)
        {
            //first time setup... will always take fucking forever (im looking at you script.wait) (referencing steam games)
            //you can use the portal pos for .isnear (might be new, or im just stupid) but this works and im too lazy to fix it. maybe next update
            if (orangeTestPed == null)
            {
                //create the reference ped, set values
                orangeTestPed = World.CreatePed(new Model(PedHash.Crow), orangePortal.Position);
                orangeTestPed.FreezePosition = true;
                orangeTestPed.IsVisible = false;
                orangeTestPed.IsInvincible = true;
                orangeTestPed.IsPersistent = true;
                Function.Call(Hash.SET_ENTITY_COLLISION, orangeTestPed, false, false);
                Wait(10);
            }
            else if (!orangeTestPed.IsNearEntity(orangePortal, new Vector3(.2f, .2f, 1.2f))) orangeTestPed.Position = orangePortal.Position;

            orangeTestPed.Position += overrideOffset;

            //return the nearby peds and vehicles
            Ped[] peds = World.GetNearbyPeds(orangeTestPed.Position, distance);
            Vehicle[] vehs = World.GetNearbyVehicles(orangeTestPed.Position, distance);

            orangeTestPed.Position = orangePortal.Position;
            Tuple<Ped[], Vehicle[]> pedsAndVehs = new Tuple<Ped[], Vehicle[]>(peds, vehs);
            return pedsAndVehs;
        }
        else
        {
            //first time setup... will always take fucking forever (im looking at you script.wait) (referencing steam games)
            //you can use the portal pos for .isnear (might be new, or im just stupid) but this works and im too lazy to fix it. maybe next update
            if (blueTestPed == null)
            {
                //create the reference ped, set values
                blueTestPed = World.CreatePed(new Model(PedHash.Crow), bluePortal.Position);
                blueTestPed.FreezePosition = true;
                blueTestPed.IsVisible = false;
                bluePortal.IsInvincible = true;
                blueTestPed.IsPersistent = true;
                Function.Call(Hash.SET_ENTITY_COLLISION, blueTestPed, false, false);
                Wait(10);
            }
            else if (!blueTestPed.IsNearEntity(bluePortal, new Vector3(.2f, .2f, 1.2f))) blueTestPed.Position = bluePortal.Position;

            blueTestPed.Position += overrideOffset;

            //return the nearby peds
            Ped[] peds = World.GetNearbyPeds(blueTestPed.Position, distance);
            Vehicle[] vehs = World.GetNearbyVehicles(blueTestPed.Position, distance);

            blueTestPed.Position = bluePortal.Position;
            Tuple<Ped[], Vehicle[]> pedsAndVehs = new Tuple<Ped[], Vehicle[]>(peds, vehs);
            return pedsAndVehs;
        }
    }

    #endregion
    
    #region Timeout
    bool IsPedOnTimeout(Ped ped, bool tpFromOrange)
    {
        //goes through each ped and sees if they're on the reqested list
        foreach (Ped op in orangeTPRecentlyPeds)
        {
            if (ped.Equals(op))
            {
                if(tpFromOrange) return true;
                return false;
            }
        }
        foreach (Ped bp in blueTPRecentlyPeds)
        {
            if (ped.Equals(bp))
            {
                if(!tpFromOrange) return true;
                return false;
            }
        }
        return false;
    }
    bool IsVehicleOnTimeout(Vehicle veh, bool tpFromOrange)
    {
        //goes through each vehicle and sees if they're on the reqested list
        foreach (Vehicle ov in orangeTpRecentlyVehicles)
        {
            if (veh.Equals(ov))
            {
                if (tpFromOrange) return true;
                return false;
            }
        }
        foreach (Vehicle bv in blueTpRecentlyVehicles)
        {
            if (veh.Equals(bv))
            {
                if (!tpFromOrange) return true;
                return false;
            }
        }
        return false;
    }
    private void RefreshPlayerTimeouts()
    {
        Ped playerPed = Game.Player.Character;
        //Resets recent TeleportPed if you go certain distance away
        if (orangePortal != null && bluePortal != null)
        {
            if (blueTeleportedRecently && !orangePortal.IsNearEntity(playerPed, new Vector3(2.5f, 2.5f, 2.5f)))
            {
                blueTeleportedRecently = false;
            }

            if (orangeTeleportedRecently && !bluePortal.IsNearEntity(playerPed, new Vector3(2.5f, 2.5f, 2.5f)))
            {
                orangeTeleportedRecently = false;
            }
        }
    }
    private void RefreshEntityTimeouts()
    {
        //goes through all the peds/vehicles on timeout, and checks if they're far enough away to get off the timeout
        if (orangeTPRecentlyPeds != null && orangeTPRecentlyPeds.Count > 0)
        {
            for (int i = orangeTPRecentlyPeds.Count - 1; i >= 0; i--)
            {
                if (!orangeTPRecentlyPeds[i].IsNearEntity(bluePortal, new Vector3(2.5f, 2.5f, 2.5f)))
                {
                    orangeTPRecentlyPeds.Remove(orangeTPRecentlyPeds[i]);
                }
            }
        }
        if (orangeTpRecentlyVehicles != null && orangeTpRecentlyVehicles.Count > 0)
        {
            for (int i = orangeTpRecentlyVehicles.Count - 1; i >= 0; i--)
            {
                if (!orangeTpRecentlyVehicles[i].IsNearEntity(bluePortal, new Vector3(3.5f, 3.5f, 3.5f)))
                {
                    orangeTpRecentlyVehicles.Remove(orangeTpRecentlyVehicles[i]);
                }
            }
        }
        if (blueTPRecentlyPeds != null && blueTPRecentlyPeds.Count > 0)
        {
            for (int i = blueTPRecentlyPeds.Count - 1; i >= 0; i--)
            {
                if (!blueTPRecentlyPeds[i].IsNearEntity(orangePortal, new Vector3(2.5f, 2.5f, 2.5f)))
                {
                    blueTPRecentlyPeds.Remove(blueTPRecentlyPeds[i]);
                }
            }
        }
        if (blueTpRecentlyVehicles != null && blueTpRecentlyVehicles.Count > 0)
        {
            for (int i = blueTpRecentlyVehicles.Count - 1; i >= 0; i--)
            {
                if (!blueTpRecentlyVehicles[i].IsNearEntity(orangePortal, new Vector3(3.5f, 3.5f, 3.5f)))
                {
                    blueTpRecentlyVehicles.Remove(blueTpRecentlyVehicles[i]);
                }
            }
        }
    }

    #endregion

    #region Grab Entity
    void GrabEntityWithPortalGun()
    {
        Ped playerPed = Game.Player.Character;
        //list of banned entities, some of which will crash game if grabbed
        List<Entity> bannedEntities = new List<Entity>{orangePortal, bluePortal, orangeTestPed, blueTestPed, playerPed};
        if (!isEntityAttached && Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING, Game.Player) && WeaponHash.HeavyPistol.GetHashCode() == playerPed.Weapons.Current.Hash.GetHashCode() && !playerPed.IsRagdoll && !playerPed.IsProne)
        {
            RaycastResult ray = GetRayFromCam();

            //if a good ray
            if (ray.DitHitEntity && ray.HitEntity.Exists() && ray.HitEntity.IsInRangeOf(playerPed.Position, 5.75f) &&
                (ray.HitEntity.Model.IsPed || ray.HitEntity.Model.IsVehicle))
            {
                //make sure it's not a banned entity
                foreach (Entity bE in bannedEntities)
                {
                    if (ray.HitEntity.Equals(bE))
                    {
                        playSound(playerInvalid);
                        return;
                    }
                }

                //playsound, attach entity, and set the attatatched entity
                playSound(playerPortalOpen1);
                attachedEntity = ray.HitEntity;
                attachedEntity.AttachTo(playerPed,
                    Function.Call<int>(Hash.GET_PED_BONE_INDEX, playerPed.Handle, Bone.IK_R_Hand.GetHashCode()),
                    new Vector3(World.GetDistance(playerPed.Position, attachedEntity.Position), 0, 0),
                    new Vector3(-78.5f, 0, 0));
                if (attachedEntity.Model.IsPed) attachedEntity.Rotation = new Vector3(-84, 0, 0);
                isEntityAttached = true;
            }
            else playSound(playerInvalid);
        }
        //if you press e and you do have an entity, let go of it.
        else if (isEntityAttached && attachedEntity != null)
        {
            LetGoOfGrabbedEntity();
        }
    }
    void LetGoOfGrabbedEntity()
    {
        //self-explanitory
        playSound(playerPortalOpen2);
        attachedEntity.Detach();
        if (attachedEntity.Model.IsPed && !attachedEntity.IsDead) Function.Call(Hash.SET_PED_TO_RAGDOLL, attachedEntity, 500, 500, 0, 1, 1, 1);
        attachedEntity.Velocity = Vector3.Zero;
        attachedEntity = null;
        isEntityAttached = false;
    }

    #endregion

    #region Suck In Moon
    void SuckInBecauseOfMoon()
    {
        //make sure they're not null, then check which one is in space
        if (orangePortal != null && bluePortal != null)
        {
            if (!orangePortalInSpace && bluePortalInspace)
            {
                CommenceTheFlinging(true, orangePortal.Position);
            }
            else if (!bluePortalInspace && orangePortalInSpace)
            {
                CommenceTheFlinging(false, bluePortal.Position);
            }
        }
    }
    private void CommenceTheFlinging(bool isOrange, Vector3 position)
    {
        //get shit tone of tuples in a spread area, then
        Tuple<Ped[], Vehicle[]> pavCenter = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(0, 0, -5));
        Tuple<Ped[], Vehicle[]> pavTop = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(0, 2 * moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavTopR = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(2 * moonSuckDistance, 2 * moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavR = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(2 * moonSuckDistance, 0, -5));
        Tuple<Ped[], Vehicle[]> pavBotR = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(2 * moonSuckDistance, -2 * moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavBot = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(0, -2 * moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavBotL = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(-2 * moonSuckDistance, -2 * moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavL = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(-2 * moonSuckDistance, 0, -5));
        Tuple<Ped[], Vehicle[]> pavTopL = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(-2 * moonSuckDistance, 2 * moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavFixTopR = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(moonSuckDistance, moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavFixBotR = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(-1 * moonSuckDistance, moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavFixBotL = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(-1 * moonSuckDistance, -1 * moonSuckDistance, -5));
        Tuple<Ped[], Vehicle[]> pavFixTopL = GetNearestPedsAndVehiclestoPortal(isOrange, moonSuckDistance, new Vector3(moonSuckDistance, -1 * moonSuckDistance, -5));

        FlingPedsFromList(new List<Ped>(pavCenter.Item1), position); //center
        FlingPedsFromList(new List<Ped>(pavTop.Item1), position); //top
        FlingPedsFromList(new List<Ped>(pavTopR.Item1), position); //topR
        FlingPedsFromList(new List<Ped>(pavR.Item1), position); //R
        FlingPedsFromList(new List<Ped>(pavBotR.Item1), position); //botR
        FlingPedsFromList(new List<Ped>(pavBot.Item1), position); //bot
        FlingPedsFromList(new List<Ped>(pavBotL.Item1), position); //botL
        FlingPedsFromList(new List<Ped>(pavL.Item1), position); //L
        FlingPedsFromList(new List<Ped>(pavTopL.Item1), position); //topL
        FlingPedsFromList(new List<Ped>(pavFixTopR.Item1), position); //fixTopR
        FlingPedsFromList(new List<Ped>(pavFixBotR.Item1), position); //fixBotR
        FlingPedsFromList(new List<Ped>(pavFixBotL.Item1), position); //fixBotL
        FlingPedsFromList(new List<Ped>(pavFixTopL.Item1), position); //fixTopL
        pedsBeingFlung.Clear();

        FlingVehiclesFromList(new List<Vehicle>(pavCenter.Item2), position); //center
        FlingVehiclesFromList(new List<Vehicle>(pavTop.Item2), position); //top
        FlingVehiclesFromList(new List<Vehicle>(pavTopR.Item2), position); //topR
        FlingVehiclesFromList(new List<Vehicle>(pavR.Item2), position); //R
        FlingVehiclesFromList(new List<Vehicle>(pavBotR.Item2), position); //botR
        FlingVehiclesFromList(new List<Vehicle>(pavBot.Item2), position); //bot
        FlingVehiclesFromList(new List<Vehicle>(pavBotL.Item2), position); //botL
        FlingVehiclesFromList(new List<Vehicle>(pavL.Item2), position); //L
        FlingVehiclesFromList(new List<Vehicle>(pavTopL.Item2), position); //topL
        FlingVehiclesFromList(new List<Vehicle>(pavFixTopR.Item2), position); //fixTopR
        FlingVehiclesFromList(new List<Vehicle>(pavFixBotR.Item2), position); //fixBotR
        FlingVehiclesFromList(new List<Vehicle>(pavFixBotL.Item2), position); //fixBotL
        FlingVehiclesFromList(new List<Vehicle>(pavFixTopL.Item2), position); //fixTopL
        vehiclesBeingFlung.Clear();
    }
    private void FlingPedsFromList(List<Ped> peds, Vector3 targetPos)
    {
        float pushMultiplier = -1f;
        foreach (Ped ped in peds)
        {
            //see if player should be sucked in
            if (ped == Game.Player.Character && !canMoonSuckIn) { }
            else if (!pedsBeingFlung.Contains(ped) && ped.Model != PedHash.Crow && !ped.IsInVehicle())
            {
                //add to list for no redundancies, make ragdoll, get force by pos difference, then fling
                pedsBeingFlung.Add(ped);
                if (!ped.IsRagdoll) Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, 1000, 1000, 0, 1, 1, 1);
                float pushX = pushMultiplier*(ped.Position.X - targetPos.X);
                float pushY = pushMultiplier*(ped.Position.Y - targetPos.Y);
                float pushZ = pushMultiplier*(ped.Position.Z - targetPos.Z);
                ped.ApplyForce(new Vector3(pushX, pushY, pushZ));
            }
        }
    }
    void FlingVehiclesFromList(List<Vehicle> vehs, Vector3 targetPos )
    {
        float pushMultiplier = -0.12f;
        foreach (Vehicle veh in vehs)
        {
            //see if player should be sucked in
            if (veh.GetPedOnSeat(VehicleSeat.Driver) == Game.Player.Character && !canMoonSuckIn) { }
            else if (!vehiclesBeingFlung.Contains(veh))
            {
                //add to list for no redundancies, get force by pos difference, then fling
                vehiclesBeingFlung.Add(veh);
                float pushX = pushMultiplier*(veh.Position.X - targetPos.X);
                float pushY = pushMultiplier*(veh.Position.Y - targetPos.Y);
                float pushZ = pushMultiplier*(veh.Position.Z - targetPos.Z);
                veh.ApplyForce(new Vector3(pushX, pushY, pushZ));
            }

        }
    }

    #endregion

    #region Moon Shot
    private bool IsPlayerLookingAtMoon()
    {
        TimeSpan currTimeYo = World.CurrentDayTime;
        int hour = currTimeYo.Hours;
        int min = currTimeYo.Minutes;

        //for this, i got the time, then by 15 min intervals, hardcoded where the player should be looking using cam rotation if he were looking at the moon
        switch (hour)
        {
            case 21:
                return IsTimeRightFromMin(min, new Vector3(9.197f, 0, -88.133f), new Vector3(9.282f, 0, -82.38f), new Vector3(9.602f, 0, -76.738f), new Vector3(9.966f, 0, -71.094f));
            case 22:
                return IsTimeRightFromMin(min, new Vector3(10.566f, 0, -65.317f), new Vector3(11.608f, 0, -59.566f), new Vector3(12.565f, 0, -53.863f), new Vector3(13.387f, 0, -48.279f));
            case 23:
                return IsTimeRightFromMin(min, new Vector3(14.672f, 0, -42.503f), new Vector3(15.894f, 0, -36.811f), new Vector3(17.172f, 0, -31.048f), new Vector3(18.541f, 0, -25.463f));
            case 0:
                return IsTimeRightFromMin(min, new Vector3(19.831f, 0, -26.829f), new Vector3(21.232f, 0, -22.94f), new Vector3(22.415f, 0, -19.1f), new Vector3(23.695f, 0, -15.315f));
            case 1:
                return IsTimeRightFromMin(min, new Vector3(24.666f, 0, -11.519f), new Vector3(25.702f, 0, -7.630f), new Vector3(26.566f, 0, -3.85f), new Vector3(27.39f, 0, -0.005f));
            case 2:
                return IsTimeRightFromMin(min, new Vector3(27.994f, 0, 3.732f), new Vector3(28.485f, 0, 7.620f), new Vector3(28.984f, 0, 11.46f), new Vector3(29.171f, 0, 15.248f));
            case 3:
                return IsTimeRightFromMin(min, new Vector3(29.191f, 0, 19.110f), new Vector3(29.079f, 0, 22.932f), new Vector3(28.984f, 0, 26.71f), new Vector3(28.626f, 0, 30.56f));
            case 4:
                return IsTimeRightFromMin(min, new Vector3(28.1f, 0, 34.353f), new Vector3(27.392f, 0, 38.182f), new Vector3(26.659f, 0, 42.022f), new Vector3(25.799f, 0, 45.87f));
            case 5:
                return IsTimeRightFromMin(min, new Vector3(24.908f, 0, 49.612f), new Vector3(23.561f, 0, 53.552f), new Vector3(22.239f, 0, 57.334f), new Vector3(21.103f, 0, 61.002f));
            default:
                return false;
        }
    }
    bool IsTimeRightFromMin(int min, Vector3 hourLook, Vector3 quarterLook, Vector3 halfLook, Vector3 threeQuarterLook)
    {
        //added big tolerances in case the moon changed pos
        float tolX = 6.5f;
        float tolZ = 6.5f;
        float minAddX = .03f;
        float minAddZ = .225f;
        Vector3 playerLook = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT);

        if (min < 15)
        {
            if (Math.Abs(playerLook.X - (hourLook.X + minAddX * min)) > tolX || Math.Abs(playerLook.Z - (hourLook.Z + minAddZ * min)) > tolZ) return false;
            return true;
        }
        if (min < 30)
        {
            if (Math.Abs(playerLook.X - (quarterLook.X + minAddX * (min - 15))) > tolX || Math.Abs(playerLook.Z - (quarterLook.Z + minAddZ * (min - 15))) > tolZ) return false;
            return true;
        }
        if (min < 45)
        {
            if (Math.Abs(playerLook.X - (halfLook.X + minAddX * (min - 30))) > tolX || Math.Abs(playerLook.Z - (halfLook.Z + minAddZ * (min - 30))) > tolZ) return false;
            return true;
        }
        if(min <= 60)
        {
            if (Math.Abs(playerLook.X - (threeQuarterLook.X + minAddX * (min - 45))) > tolX || Math.Abs(playerLook.Z - (threeQuarterLook.Z + minAddZ * (min - 45))) > tolZ) return false;
            return true;
        }
        return false;
    }

    #endregion

    #region Misc
    void playSound(SoundPlayer playasGotta)
    {
        //if not all sounds are loaded, dont play any sounds
        if (!allSoundsLoaded) return;
        //make a new task to play the sound. honestly not sure if this does anything
        Task t = new Task(() =>
        {
          playasGotta.Play();  
        });
        t.Start();
    }
    private static float MakePositive(float f)
    {
        //make plus not minus
        if (f < 0)
        {
            f *= -1;
        }
        return f;
    }
    private void Plug()
    {
        //better plug myself so that people know me
        if (Game.Player.IsPlaying && !hasPlugged)
        {
            GTA.UI.Notify("Portal Gun " + version + "\nBy Pandassaurus");
            hasPlugged = true;
        }
    }
    RaycastResult GetRayFromCam()
    {
        //put all the raycast code in one function. so much nicer.
        Vector3 camPos = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
        Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT);
        float retz = camRot.Z * 0.0174532924F;
        float retx = camRot.X * 0.0174532924F;
        float absx = (float)Math.Abs(Math.Cos(retx));
        Vector3 camStuff = new Vector3((float)Math.Sin(retz) * absx * -1, (float)Math.Cos(retz) * absx, (float)Math.Sin(retx));
        return World.Raycast(camPos, camPos + camStuff * 1000, IntersectOptions.Everything);
        //cock in the mouth
    }

    #endregion

    //Rest in piece beautiful code. Effort was put in to make you work, but alas, you were replaced by something that was already there
    //Goodbye, friend...
    //[Deleted]
}