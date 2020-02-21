using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Stollie.Medbay
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CryoChamber), false, "Medicalstationbed_Large", "Medicalstationbed_Small")]
    public class MedBay : MyGameLogicComponent
    {
        MyObjectBuilder_EntityBase objectBuilder = null;
        IMyCryoChamber medBay = null;
        int tick = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                this.objectBuilder = objectBuilder;
                medBay = Entity as IMyCryoChamber;
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Init Error" + e, 10000, "Red");
            }
        }
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return objectBuilder;
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MyAPIGateway.Session == null)
                    return;

                if (!MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
                    return;

                // Check if medbay is working and is occupied.
                if (medBay.IsWorking && medBay.ControllerInfo.ControllingIdentityId != 0)
                {
                    long occupantEntityId = medBay.ControllerInfo.ControllingIdentityId;
                    IMyPlayer occupantPlayer = null;
                    float occupantHealth = MyVisualScriptLogicProvider.GetPlayersHealth(occupantEntityId);

                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);
                    foreach (var player in players)
                    {
                        if (player.IdentityId == occupantEntityId)
                        {
                            occupantPlayer = player;
                        }
                    }

                    MyEntityStat healthStat = null;
                    MyCharacterStatComponent stats = occupantPlayer.Character.Components.Get<MyEntityStatComponent>() as MyCharacterStatComponent;
                    if (stats != null)
                    {
                        MyStringHash healthId = MyStringHash.GetOrCompute("Health");
                        bool statsDict = stats.TryGetStat(healthId, out healthStat);
                    }

                    if (tick % 50 == 0 && occupantHealth < healthStat.MaxValue)
                    {
                        float amountToHeal = healthStat.MaxValue / 10;

                        if (healthStat.MaxValue - occupantHealth < amountToHeal)
                        {
                            MyVisualScriptLogicProvider.SetPlayersHealth(occupantEntityId, healthStat.MaxValue);
                        }
                        else
                        {
                            MyVisualScriptLogicProvider.SetPlayersHealth(occupantEntityId, occupantHealth += amountToHeal);
                        }
                        
                        //MyVisualScriptLogicProvider.SendChatMessage(occupantHealth.ToString(), "PlayerHP (DEBUG): ");
                    }
                    
                    if (tick > 1001) tick = 0;
                    
                    tick++;
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 2500, "Red");
            }
        }
    }
}
