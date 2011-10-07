﻿/*
 * Copyright (C) 2011 mooege project
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System.Linq;
using System.Collections.Generic;
using Mooege.Common;
using Mooege.Core.Common.Toons;
using Mooege.Core.Common.Items;
using Mooege.Core.GS.Game;
using Mooege.Core.GS.Objects;
using Mooege.Core.GS.Map;
using Mooege.Core.GS.Actors;
using Mooege.Core.GS.Skills;
using Mooege.Net.GS;
using Mooege.Net.GS.Message;
using Mooege.Net.GS.Message.Fields;
using Mooege.Net.GS.Message.Definitions.ACD;
using Mooege.Net.GS.Message.Definitions.Act;
using Mooege.Net.GS.Message.Definitions.Attribute;
using Mooege.Net.GS.Message.Definitions.Connection;
using Mooege.Net.GS.Message.Definitions.Combat;
using Mooege.Net.GS.Message.Definitions.Game;
using Mooege.Net.GS.Message.Definitions.Hero;
using Mooege.Net.GS.Message.Definitions.Misc;
using Mooege.Net.GS.Message.Definitions.Player;
using Mooege.Net.GS.Message.Definitions.Skill;
using Mooege.Net.GS.Message.Definitions.Inventory;
using Mooege.Net.GS.Message.Definitions.World;

// TODO: Player should use a message queue and only flush to socket when a tick is finished

namespace Mooege.Core.GS.Player
{
    public class Player : Actor
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public override ActorType ActorType { get { return ActorType.Player; } }

        public GameClient InGameClient { get; set; }

        public Toon Properties { get; private set; }
        public SkillSet SkillSet;
        public Inventory Inventory;

        public Dictionary<uint, IRevealable> RevealedObjects { get; private set; }

        // Collection of items that only the player can see. This is only used when items drop from killing an actor
        // TODO: Might want to just have a field on the item itself to indicate whether it is visible to only one player
        public Dictionary<uint, Item> GroundItems { get; private set; }

        public Player(World world, GameClient client, Toon bnetToon)
            : base(world, world.NewPlayerID)
        {
            this.InGameClient = client;

            this.Properties = bnetToon;
            this.Inventory = new Inventory(this);
            this.SkillSet = new Skills.SkillSet(this.Properties.Class);

            this.RevealedObjects = new Dictionary<uint, IRevealable>();
            this.GroundItems = new Dictionary<uint, Item>();

            // actor values
            this.ActorSNO = this.ClassSNO;
            this.Field2 = 0x00000009;
            this.Field3 = 0x00000000;
            this.Scale = ModelScale;
            this.RotationAmount = 0.05940768f;
            this.RotationAxis = new Vector3D(0f, 0f, 0.9982339f);

            this.Position.X = 3143.75f;
            this.Position.Y = 2828.75f;
            this.Position.Z = 59.075588f;

            // den of evil: this.Position.X = 2526.250000f; this.Position.Y = 2098.750000f; this.Position.Z = -5.381495f;
            // inn: this.Position.X = 2996.250000f; this.Position.Y = 2793.750000f; this.Position.Z = 24.045330f;
            // adrias hut: this.Position.X = 1768.750000f; this.Position.Y = 2921.250000f; this.Position.Z = 20.333143f;
            // cemetery of the forsaken: this.Position.X = 2041.250000f; this.Position.Y = 1778.750000f; this.Position.Z = 0.426203f;
            // defiled crypt level 2: this.WorldId = 2000289804; this.Position.X = 158.750000f; this.Position.Y = 76.250000f; this.Position.Z = 0.100000f;

            this.GBHandle.Type = (int)GBHandleType.Player;
            this.GBHandle.GBID = this.Properties.ClassID;

            this.Field7 = -1;
            this.Field8 = -1;
            this.Field9 = 0x00000000;
            this.Field10 = 0x0;

            #region Attributes
            this.Attributes[GameAttribute.SkillKit] = this.SkillKit;
            this.Attributes[GameAttribute.Buff_Active, 0x33C40] = true;
            this.Attributes[GameAttribute.Skill, 0x7545] = 1;
            this.Attributes[GameAttribute.Skill_Total, 0x7545] = 1;
            this.Attributes[GameAttribute.Resistance_Total, 0x226] = 0.5f;
            this.Attributes[GameAttribute.Resistance, 0x226] = 0.5f;
            this.Attributes[GameAttribute.Immobolize] = true;
            this.Attributes[GameAttribute.Untargetable] = true;
            this.Attributes[GameAttribute.Skill_Total, 0x76B7] = 1;
            this.Attributes[GameAttribute.Skill, 0x76B7] = 1;
            this.Attributes[GameAttribute.Skill, 0x6DF] = 1;
            this.Attributes[GameAttribute.Buff_Active, 0xCE11] = true;
            this.Attributes[GameAttribute.CantStartDisplayedPowers] = true;
            this.Attributes[GameAttribute.Skill_Total, 0x216FA] = 1;
            this.Attributes[GameAttribute.Skill, 0x176C4] = 1;
            this.Attributes[GameAttribute.Skill, 0x216FA] = 1;
            this.Attributes[GameAttribute.Skill_Total, 0x176C4] = 1;
            this.Attributes[GameAttribute.Skill_Total, 0x6DF] = 1;
            this.Attributes[GameAttribute.Resistance, 0xDE] = 0.5f;
            this.Attributes[GameAttribute.Resistance_Total, 0xDE] = 0.5f;
            this.Attributes[GameAttribute.Get_Hit_Recovery] = 6f;
            this.Attributes[GameAttribute.Get_Hit_Recovery_Per_Level] = 1f;
            this.Attributes[GameAttribute.Get_Hit_Recovery_Base] = 5f;
            this.Attributes[GameAttribute.Skill, 0x7780] = 1;
            this.Attributes[GameAttribute.Get_Hit_Max] = 60f;
            this.Attributes[GameAttribute.Skill_Total, 0x7780] = 1;
            this.Attributes[GameAttribute.Get_Hit_Max_Per_Level] = 10f;
            this.Attributes[GameAttribute.Get_Hit_Max_Base] = 50f;
            this.Attributes[GameAttribute.Resistance_Total, 0] = 3.051758E-05f; // im pretty sure key = 0 doesnt do anything since the lookup is (attributeId | (key << 12)), maybe this is some base resistance? /cm
            this.Attributes[GameAttribute.Resistance_Total, 1] = 3.051758E-05f;
            this.Attributes[GameAttribute.Resistance_Total, 2] = 3.051758E-05f;
            this.Attributes[GameAttribute.Resistance_Total, 3] = 3.051758E-05f;
            this.Attributes[GameAttribute.Resistance_Total, 4] = 3.051758E-05f;
            this.Attributes[GameAttribute.Resistance_Total, 5] = 3.051758E-05f;
            this.Attributes[GameAttribute.Resistance_Total, 6] = 3.051758E-05f;
            this.Attributes[GameAttribute.Dodge_Rating_Total] = 3.051758E-05f;
            this.Attributes[GameAttribute.IsTrialActor] = true;
            this.Attributes[GameAttribute.Buff_Visual_Effect, 0xFFFFF] = true;
            this.Attributes[GameAttribute.Crit_Percent_Cap] = 0x3F400000;
            this.Attributes[GameAttribute.Resource_Cur, this.ResourceID] = 200f;
            this.Attributes[GameAttribute.Resource_Max, this.ResourceID] = 200f;
            this.Attributes[GameAttribute.Resource_Max_Total, this.ResourceID] = 200f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_All] = 2f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_All] = 1f;
            this.Attributes[GameAttribute.Resource_Regen_Total, this.ResourceID] = 3.051758E-05f;
            this.Attributes[GameAttribute.Resource_Effective_Max, this.ResourceID] = 200f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 0xFFFFF] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Total, 0xFFFFF] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 0xFFFFF] = 3.051758E-05f;
            this.Attributes[GameAttribute.Attacks_Per_Second_Item_CurrentHand] = 1.199219f;
            this.Attributes[GameAttribute.Attacks_Per_Second_Item_Total_MainHand] = 1.199219f;
            this.Attributes[GameAttribute.Attacks_Per_Second_Total] = 1.199219f;
            this.Attributes[GameAttribute.Attacks_Per_Second] = 1f;
            this.Attributes[GameAttribute.Attacks_Per_Second_Item_MainHand] = 1.199219f;
            this.Attributes[GameAttribute.Attacks_Per_Second_Item_Total] = 1.199219f;
            this.Attributes[GameAttribute.Buff_Icon_End_Tick0, 0x00033C40] = 0x000003FB;
            this.Attributes[GameAttribute.Attacks_Per_Second_Item_Subtotal] = 3.051758E-05f;
            this.Attributes[GameAttribute.Attacks_Per_Second_Item] = 3.051758E-05f;
            this.Attributes[GameAttribute.Buff_Icon_Start_Tick0, 0x00033C40] = 0x00000077;
            this.Attributes[GameAttribute.Hit_Chance] = 1f;
            this.Attributes[GameAttribute.Casting_Speed_Total] = 1f;
            this.Attributes[GameAttribute.Casting_Speed] = 1f;
            this.Attributes[GameAttribute.Movement_Scalar_Total] = 1f;
            this.Attributes[GameAttribute.Skill_Total, 0x0002EC66] = 0;
            this.Attributes[GameAttribute.Movement_Scalar_Capped_Total] = 1f;
            this.Attributes[GameAttribute.Movement_Scalar_Subtotal] = 1f;
            this.Attributes[GameAttribute.Strafing_Rate_Total] = 3.051758E-05f;
            this.Attributes[GameAttribute.Sprinting_Rate_Total] = 3.051758E-05f;
            this.Attributes[GameAttribute.Running_Rate_Total] = 0.3598633f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_MainHand, 0] = 2f;
            this.Attributes[GameAttribute.Walking_Rate_Total] = 0.2797852f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_MainHand, 0] = 1f;
            this.Attributes[GameAttribute.Damage_Delta_Total, 1] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Delta_Total, 2] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Delta_Total, 3] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Delta_Total, 4] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Delta_Total, 5] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Delta_Total, 6] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Delta_Total, 0] = 1f;
            this.Attributes[GameAttribute.Running_Rate] = 0.3598633f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 1] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 2] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 3] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 4] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 5] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 6] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total_CurrentHand, 0] = 2f;
            this.Attributes[GameAttribute.Walking_Rate] = 0.2797852f;
            this.Attributes[GameAttribute.Damage_Min_Total, 1] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Total, 2] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Total, 3] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Total, 4] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Total, 5] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Total, 6] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_CurrentHand, 1] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_CurrentHand, 2] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_CurrentHand, 3] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_CurrentHand, 4] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_CurrentHand, 5] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_CurrentHand, 6] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Total, 0] = 2f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total_CurrentHand, 0] = 1f;
            this.Attributes[GameAttribute.Movement_Scalar] = 1f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 1] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 2] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 3] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 4] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 5] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 6] = 3.051758E-05f;
            this.Attributes[GameAttribute.Damage_Min_Subtotal, 0] = 2f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta, 0] = 1f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_SubTotal, 0] = 1f;
            this.Attributes[GameAttribute.Damage_Weapon_Max, 0] = 3f;
            this.Attributes[GameAttribute.Damage_Weapon_Max_Total, 0] = 3f;
            this.Attributes[GameAttribute.Damage_Weapon_Delta_Total, 0] = 1f;
            this.Attributes[GameAttribute.Trait, 0x0000CE11] = 1;
            this.Attributes[GameAttribute.Damage_Weapon_Min, 0] = 2f;
            this.Attributes[GameAttribute.Damage_Weapon_Min_Total, 0] = 2f;
            this.Attributes[GameAttribute.Skill, 0x0000CE11] = 1;
            this.Attributes[GameAttribute.Skill_Total, 0x0000CE11] = 1;
            this.Attributes[GameAttribute.Resource_Type_Primary] = this.ResourceID;
            this.Attributes[GameAttribute.Hitpoints_Max_Total] = 76f;
            this.Attributes[GameAttribute.Hitpoints_Max] = 40f;
            this.Attributes[GameAttribute.Hitpoints_Total_From_Level] = 3.051758E-05f;
            this.Attributes[GameAttribute.Hitpoints_Total_From_Vitality] = 36f;
            this.Attributes[GameAttribute.Hitpoints_Factor_Vitality] = 4f;
            this.Attributes[GameAttribute.Hitpoints_Factor_Level] = 4f;
            this.Attributes[GameAttribute.Hitpoints_Cur] = 76f;
            this.Attributes[GameAttribute.Disabled] = true;
            this.Attributes[GameAttribute.Loading] = true;
            this.Attributes[GameAttribute.Invulnerable] = true;
            this.Attributes[GameAttribute.TeamID] = 2;
            this.Attributes[GameAttribute.Skill_Total, 0xFFFFF] = 1;
            this.Attributes[GameAttribute.Skill, 0xFFFFF] = 1;
            this.Attributes[GameAttribute.Buff_Icon_Count0, 0x0000CE11] = 1;
            this.Attributes[GameAttribute.Hidden] = true;
            this.Attributes[GameAttribute.Level_Cap] = 13;
            this.Attributes[GameAttribute.Level] = this.Properties.Level;
            this.Attributes[GameAttribute.Experience_Next] = 1200;
            this.Attributes[GameAttribute.Experience_Granted] = 1000;
            this.Attributes[GameAttribute.Armor_Total] = 0;
            this.Attributes[GameAttribute.Defense] = 10f;
            this.Attributes[GameAttribute.Buff_Icon_Count0, 0x00033C40] = 1;
            this.Attributes[GameAttribute.Vitality] = 9f;
            this.Attributes[GameAttribute.Precision] = 11f;
            this.Attributes[GameAttribute.Attack] = 10f;
            this.Attributes[GameAttribute.Shared_Stash_Slots] = 14;
            this.Attributes[GameAttribute.Backpack_Slots] = 60;
            this.Attributes[GameAttribute.General_Cooldown] = 0;
            #endregion // Attributes

            this.World.Enter(this); // Enter only once all fields have been initialized to prevent a run condition
        }

        public void Consume(GameClient client, GameMessage message)
        {
            if (message is AssignActiveSkillMessage) OnAssignActiveSkill(client, (AssignActiveSkillMessage)message);
            else if (message is AssignPassiveSkillMessage) OnAssignPassiveSkill(client, (AssignPassiveSkillMessage)message);
            else if (message is PlayerChangeHotbarButtonMessage) OnPlayerChangeHotbarButtonMessage(client, (PlayerChangeHotbarButtonMessage)message);
            else if (message is TargetMessage) OnObjectTargeted(client, (TargetMessage)message);
            else return;

            UpdateState();
            client.FlushOutgoingBuffer();
        }

        // TODO: This needs to be cleaned up
        /// <summary>
        /// Greets the player and sends the client initial data it needs to get in-game.
        /// </summary>
        /// <param name="message"></param>
        public void Greet(JoinBNetGameMessage message)
        {
            Logger.Trace("Greeting player {0} and positioning him to {1}", this.Properties.Name, this.Position);

            // send versions message
            InGameClient.SendMessageNow(new VersionsMessage(message.SNOPackHash));

            // send connection established message.
            InGameClient.SendMessage(new ConnectionEstablishedMessage
            {
                Field0 = 0x00000000,
                Field1 = 0x4BB91A16,
                SNOPackHash = message.SNOPackHash,
            });

            // game setup message.
            InGameClient.SendMessage(new GameSetupMessage
            {
                Field0 = 0x00000077,
            });

            InGameClient.SendMessage(new SavePointInfoMessage
            {
                snoLevelArea = -1,
            });

            InGameClient.SendMessage(new HearthPortalInfoMessage
            {
                snoLevelArea = -1,
                Field1 = -1,
            });

            // transition player to act so client can load act related data? /raist
            InGameClient.SendMessage(new ActTransitionMessage
            {
                Field0 = 0x00000000,
                Field1 = true,
            });

            if (this.World != null)
                this.World.Reveal(this);

            // Notify the client of the new player
            InGameClient.SendMessage(new NewPlayerMessage
            {
                Field0 = 0x00000000, //Party frame (0x00000000 hide, 0x00000001 show)
                Field1 = "", //Owner name?
                ToonName = this.Properties.Name,
                Field3 = 0x00000002, //party frame class
                Field4 = 0x00000004, //party frame level
                snoActorPortrait = this.ClassSNO, //party frame portrait
                Field6 = 0x00000001,
                StateData = this.GetStateData(),
                Field8 = false, //announce party join
                Field9 = 0x00000001,
                ActorID = this.DynamicID,
            });

            // reveal the hero
            this.Reveal(this);

            InGameClient.SendMessage(new ACDCollFlagsMessage
            {
                ActorID = this.DynamicID,
                CollFlags = 0x00000000,
            });

            this.Attributes.SendMessage(InGameClient, this.DynamicID);

            InGameClient.SendMessage(new ACDGroupMessage()
            {
                ActorID = this.DynamicID,
                Field1 = -1,
                Field2 = -1,
            });

            InGameClient.SendMessage(new ANNDataMessage(Opcodes.ANNDataMessage7)
            {
                ActorID = this.DynamicID,
            });

            InGameClient.SendMessage(new ACDTranslateFacingMessage(Opcodes.ACDTranslateFacingMessage1)
            {
                ActorID = this.DynamicID,
                Angle = 3.022712f,
                Field2 = false,
            });

            InGameClient.SendMessage(new PlayerEnterKnownMessage()
            {
                Field0 = 0x00000000,
                PlayerID = this.DynamicID,
            });

            InGameClient.SendMessage(new PlayerActorSetInitialMessage()
            {
                PlayerID = this.DynamicID,
                Field1 = 0x00000000,
            });

            InGameClient.SendMessage(new SNONameDataMessage()
            {
                Name = new SNOName()
                {
                    Group = 0x00000001,
                    Handle = this.ClassSNO,
                },
            });
            InGameClient.FlushOutgoingBuffer();

            InGameClient.SendMessage(new DWordDataMessage() // TICK
            {
                Id = 0x0089,
                Field0 = 0x00000077,
            });
            InGameClient.FlushOutgoingBuffer();

            // FIXME: hackedy hack
            var attribs = new GameAttributeMap();
            attribs[GameAttribute.Hitpoints_Healed_Target] = 76f;
            attribs.SendMessage(InGameClient, this.DynamicID);

            InGameClient.SendMessage(new DWordDataMessage() // TICK
            {
                Id = 0x0089,
                Field0 = 0x0000007D,
            });
            InGameClient.FlushOutgoingBuffer();
        }

        public override void OnEnter(World world)
        {
            // FIXME: Hardcoded crap
            // Player enters world
            this.InGameClient.SendMessage(new EnterWorldMessage()
            {
                EnterPosition = this.Position,
                WorldID = this.DynamicID,
                WorldSNO = this.World.WorldSNO,
            });
            this.InGameClient.SendMessage(new PlayerWarpedMessage()
            {
                Field0 = 9,
                Field1 = 0f,
            });
            this.InGameClient.PacketId += 40 * 2;
            this.InGameClient.SendMessage(new DWordDataMessage()
            {
                Id = 0x89,
                Field0 = this.InGameClient.PacketId,
            });
            this.InGameClient.FlushOutgoingBuffer();
        }

        public override void OnLeave(World world)
        {
        }

        // Message handlers
        private void OnObjectTargeted(GameClient client, TargetMessage message)
        {
            // TODO: Should just have an OnTargeted method on Actor and call it from here
            //Logger.Info("Player interaction with {0}", message.AsText());
            Portal p = this.World.GetPortal(message.TargetID);
            if (p != null)
            {
                // Player clicked a portal
                World world = this.World.Game.GetWorld(p.Destination.WorldSNO);
                if (world != null)
                    this.TransferTo(world, p.TargetPos);
                else
                    Logger.Warn("Portal's destination world does not exist (WorldSNO = {0})", p.Destination.WorldSNO);
                return;
            }

            Item item = this.World.GetItem(message.TargetID);
            if (item != null)
            {
                // Player clicked an item
                if (this.Inventory.PickUp(item))
                {
                    if (this.GroundItems.ContainsKey(item.DynamicID))
                        this.GroundItems.Remove(item.DynamicID);
                }
                return;
            }
        }

        private void OnPlayerChangeHotbarButtonMessage(GameClient client, PlayerChangeHotbarButtonMessage message)
        {
            this.SkillSet.HotBarSkills[message.BarIndex] = message.ButtonData;
        }

        private void OnAssignPassiveSkill(GameClient client, AssignPassiveSkillMessage message)
        {
            this.SkillSet.PassiveSkills[message.SkillIndex] = message.SNOSkill;
        }

        private void OnAssignActiveSkill(GameClient client, AssignActiveSkillMessage message)
        {
            var oldSNOSkill = this.SkillSet.ActiveSkills[message.SkillIndex]; // find replaced skills SNO.

            foreach (HotbarButtonData button in this.SkillSet.HotBarSkills.Where(button => button.SNOSkill == oldSNOSkill)) // loop through hotbar and replace the old skill with new one
            {
                button.SNOSkill = message.SNOSkill;
            }

            this.SkillSet.ActiveSkills[message.SkillIndex] = message.SNOSkill;
        }

        public void UpdateState()
        {
            this.InGameClient.SendMessage(new HeroStateMessage
            {
                State = this.GetStateData()
            });

            this.InGameClient.PacketId += 10 * 2;
            this.InGameClient.SendMessage(new DWordDataMessage()
            {
                Id = 0x89,
                Field0 = this.InGameClient.PacketId,
            });
        }

        // Properties

        public HeroStateData GetStateData()
        {
            return new HeroStateData()
            {
                Field0 = 0x00000000,
                Field1 = 0x00000000,
                Field2 = 0x00000000,
                Gender = Properties.Gender,
                PlayerSavedData = this.GetSavedData(),
                Field5 = 0x00000000,
                tQuestRewardHistory = QuestRewardHistory,
            };
        }

        private PlayerSavedData GetSavedData()
        {
            return new PlayerSavedData()
            {
                HotBarButtons = this.SkillSet.HotBarSkills,
                SkilKeyMappings = this.SkillKeyMappings,

                Field2 = 0x00000000,
                Field3 = 0x00000001,

                Field4 = new HirelingSavedData()
                {
                    HirelingInfos = this.HirelingInfo,
                    Field1 = 0x00000000,
                    Field2 = 0x00000000,
                },

                Field5 = 0x00000000,

                LearnedLore = this.LearnedLore,
                snoActiveSkills = this.SkillSet.ActiveSkills,
                snoTraits = this.SkillSet.PassiveSkills,
                Field9 = new SavePointData() { snoWorld = -1, Field1 = -1, },
                m_SeenTutorials = this.SeenTutorials,
            };
        }

        public VisualInventoryMessage GetVisualInventory()
        {
            return new VisualInventoryMessage
            {
                ActorID = this.DynamicID,
                EquipmentList =
                    new VisualEquipment
                    {
                        Equipment =
                            Properties.Equipment.VisualItemList.Select(
                                equipment =>
                                new VisualItem
                                {
                                    GbId = equipment.Gbid,
                                    Field1 = 0x0,
                                    Field2 = 0x0,
                                    Field3 = -1
                                }).ToArray()
                    }
            };
        }

        public int ClassSNO
        {
            get
            {
                if (this.Properties.Gender == 0)
                {
                    switch (this.Properties.Class)
                    {
                        case ToonClass.Barbarian:
                            return 0x0CE5;
                        case ToonClass.DemonHunter:
                            return 0x0125C7;
                        case ToonClass.Monk:
                            return 0x1271;
                        case ToonClass.WitchDoctor:
                            return 0x1955;
                        case ToonClass.Wizard:
                            return 0x1990;
                    }
                }
                else
                {
                    switch (this.Properties.Class)
                    {
                        case ToonClass.Barbarian:
                            return 0x0CD5;
                        case ToonClass.DemonHunter:
                            return 0x0123D2;
                        case ToonClass.Monk:
                            return 0x126D;
                        case ToonClass.WitchDoctor:
                            return 0x1951;
                        case ToonClass.Wizard:
                            return 0x197E;
                    }
                }
                return 0x0;
            }
        }

        public float ModelScale
        {
            get
            {
                //dummy values, need confirmation from dump
                switch (this.Properties.Class)
                {
                    case ToonClass.Barbarian:
                        return 1.22f;
                    case ToonClass.DemonHunter:
                        return 1.43f;
                    case ToonClass.Monk:
                        return 1.43f;
                    case ToonClass.WitchDoctor:
                        return 1.43f;
                    case ToonClass.Wizard:
                        return 1.43f;
                }
                return 1.43f;
            }
        }

        public int ResourceID
        {
            get
            {
                switch (this.Properties.Class)
                {
                    case ToonClass.Barbarian:
                        return 0x00000002;
                    case ToonClass.DemonHunter:
                        return 0x00000005;
                    case ToonClass.Monk:
                        return 0x00000003;
                    case ToonClass.WitchDoctor:
                        return 0x00000000;
                    case ToonClass.Wizard:
                        return 0x00000001;
                }
                return 0x00000000;
            }
        }

        public int SkillKit
        {
            get
            {
                switch (this.Properties.Class)
                {
                    case ToonClass.Barbarian:
                        return 0x00008AF4;
                    case ToonClass.DemonHunter:
                        return 0x00008AFC;
                    case ToonClass.Monk:
                        return 0x00008AFA;
                    case ToonClass.WitchDoctor:
                        return 0x00008AFF;
                    case ToonClass.Wizard:
                        return 0x00008B00;
                }
                return 0x00000001;
            }
        }

        public SkillKeyMapping[] SkillKeyMappings = new SkillKeyMapping[15]
        {
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
            new SkillKeyMapping { Power = -1, Field1 = -1, Field2 = 0x00000000, },
        };

        public LearnedLore LearnedLore = new LearnedLore()
        {
            Field0 = 0x00000000,
            m_snoLoreLearned = new int[256]
             {
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,
                0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000,0x00000000
             },
        };

        public int[] SeenTutorials = new int[64]
        {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        };

        public PlayerQuestRewardHistoryEntry[] QuestRewardHistory = new PlayerQuestRewardHistoryEntry[0] { };

        public HirelingInfo[] HirelingInfo = new HirelingInfo[4]
        {
            new HirelingInfo { Field0 = 0x00000000, Field1 = -1, Field2 = 0x00000000, Field3 = 0x00000000, Field4 = false, Field5 = -1, Field6 = -1, Field7 = -1, Field8 = -1, },
            new HirelingInfo { Field0 = 0x00000000, Field1 = -1, Field2 = 0x00000000, Field3 = 0x00000000, Field4 = false, Field5 = -1, Field6 = -1, Field7 = -1, Field8 = -1, },
            new HirelingInfo { Field0 = 0x00000000, Field1 = -1, Field2 = 0x00000000, Field3 = 0x00000000, Field4 = false, Field5 = -1, Field6 = -1, Field7 = -1, Field8 = -1, },
            new HirelingInfo { Field0 = 0x00000000, Field1 = -1, Field2 = 0x00000000, Field3 = 0x00000000, Field4 = false, Field5 = -1, Field6 = -1, Field7 = -1, Field8 = -1, },
        };
    }
}
