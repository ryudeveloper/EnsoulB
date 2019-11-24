using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPrediction;
using static SPrediction.MinionManager;
using MinionTypes = EnsoulSharp.SDK.MinionTypes;
using EnsoulSharp.SDK.Utility;
using System.Drawing;
using MinionTeam = SPrediction.MinionManager.MinionTeam;
using MinionOrderTypes = SPrediction.MinionManager.MinionOrderTypes;

namespace RyusungAIO.Champions
{
    class Varus
    {
        private static Menu Menu;


        #region Public Methods and Operators
        internal enum Spells
        {
            Q,

            W,

            E,

            R
        }

        private static bool CanCastQE = true;
        private static float ChargingStart = 0;

        private static void Initialize()
        {
            Menu = new Menu("Varus", "Ryusung.Varus", true);

            var cMenu = new Menu("Combo", "Combo");

            cMenu.Add(new MenuBool("ElVarus.Combo.Q", "Use Q"));
            cMenu.Add(new MenuBool("ElVarus.combo.always.Q", "always Q", false));
            cMenu.Add(new MenuBool("ElVarus.Combo.E", "Use E"));
            cMenu.Add(new MenuBool("ElVarus.Combo.R", "Use R"));
            cMenu.Add(new MenuBool("ElVarus.Combo.W.Focus", "Focus W target", false));
            //cMenu.Add(new MenuBool("ElVarus.sssss", ""));
            cMenu.Add(new MenuSlider("ElVarus.Combo.R.Count", "R when enemies >= ", 1, 1, 5));
            cMenu.Add(new MenuSlider("ElVarus.Combo.Stack.Count", "Q,E when stacks >= ", 3, 1, 3));
            //cMenu.Add(new MenuBool("ElVarus.sssssssss", ""));
            cMenu.Add(
                new MenuKeyBind("ElVarus.SemiR", "Semi-manual cast R key", System.Windows.Forms.Keys.T, KeyBindType.Press));

            //cMenu.Add(new MenuBool("ElVarus.ssssssssssss", ""));
            cMenu.Add(new MenuKeyBind("ComboActive", "Combo!", System.Windows.Forms.Keys.Space, KeyBindType.Press));
            Menu.Add(cMenu);

            var hMenu = new Menu("Harass", "Harass");
            hMenu.Add(new MenuBool("ElVarus.Harass.Q", "Use Q"));
            hMenu.Add(new MenuBool("ElVarus.Harass.E", "Use E"));
            hMenu.Add(new MenuSlider("minmanaharass", "Mana needed to clear ", 55));

            Menu.Add(hMenu);

            var itemMenu = new Menu("Items", "Items");
            itemMenu.Add(new MenuBool("ElVarus.Items.Youmuu", "Use Youmuu's Ghostblade"));
            itemMenu.Add(new MenuBool("ElVarus.Items.Cutlass", "Use Cutlass"));
            itemMenu.Add(new MenuBool("ElVarus.Items.Blade", "Use Blade of the Ruined King"));
            itemMenu.Add(
                new MenuSlider("ElVarus.Items.Blade.EnemyEHP", "Enemy HP Percentage", 80, 100, 0));
            itemMenu.Add(
                new MenuSlider("ElVarus.Items.Blade.EnemyMHP", "My HP Percentage", 80, 100, 0));

            Menu.Add(itemMenu);

            var lMenu = new Menu("Clear", "Clear");
            lMenu.Add(new MenuBool("useQFarm", "Use Q"));
            lMenu.Add(
                new MenuSlider("ElVarus.Count.Minions", "Killable minions with Q >=", 2, 1, 5));
            lMenu.Add(new MenuBool("useEFarm", "Use E"));
            lMenu.Add(
                new MenuSlider("ElVarus.Count.Minions.E", "Killable minions with E >=", 2, 1, 5));
            //lMenu.Add(new MenuBool("useEFarmddsddaadsd", ""));
            lMenu.Add(new MenuBool("useQFarmJungle", "Use Q in jungle"));
            lMenu.Add(new MenuBool("useEFarmJungle", "Use E in jungle"));
            //lMenu.Add(new MenuBool("useEFarmddssd", ""));
            lMenu.Add(new MenuSlider("minmanaclear", "Mana needed to clear ", 55));

            Menu.Add(lMenu);

            //ElSinged.Misc
            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.Add(new MenuBool("ElVarus.Draw.off", "Turn drawings off"));
            miscMenu.Add(new MenuBool("ElVarus.Draw.Q", "Draw Q"));
            miscMenu.Add(new MenuBool("ElVarus.Draw.W", "Draw W"));
            miscMenu.Add(new MenuBool("ElVarus.Draw.E", "Draw E"));

            miscMenu.Add(new MenuBool("ElVarus.KSSS", "Killsteal"));

            Menu.Add(miscMenu);

            //Here comes the moneyyy, money, money, moneyyyy
            var credits = new Menu("Credits", "credits: jQuery");
            Menu.Add(credits);

            Menu.Attach();
        }

        #endregion

        private static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                 { Spells.Q, new Spell(SpellSlot.Q, 925) },
                                                                 { Spells.W, new Spell(SpellSlot.W, 0) },
                                                                 { Spells.E, new Spell(SpellSlot.E, 925) },
                                                                 { Spells.R, new Spell(SpellSlot.R, 1075) }
                                                             };


        #region Public Properties

        private static AIHeroClient Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        #endregion

        #region Public Methods and Operators

        public Varus()
        {

            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Varus credit jQuery"));
            spells[Spells.Q].SetSkillshot(.25f, 70f, 1850f, false, false, SkillshotType.Line);
            spells[Spells.E].SetSkillshot(0.35f, 120, 1500, false, false, SkillshotType.Circle);
            spells[Spells.R].SetSkillshot(.25f, 120f, 1850f, false, false, SkillshotType.Line);

            spells[Spells.Q].SetCharged("VarusQLaunch", "VarusQLaunch", 925, 1600, 2);
            Initialize();
            EnsoulSharp.SDK.Events.Tick.OnTick += OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AIBaseClient.OnDoCast += OnDoCast;
            AIBaseClient.OnBuffGain += AIBaseClientBuffGain;
            AIBaseClient.OnBuffLose += AIBaseClientBuffLose;
        }

        private void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if(sender.IsMe && (args.Slot == SpellSlot.E || args.Slot == SpellSlot.Q))
            {
                if(args.Slot == SpellSlot.Q)
                {
                    DelayAction.Add(1250, () => { CanCastQE = true; });
                } else
                {
                    DelayAction.Add(1000, () => { CanCastQE = true; });
                }
            }
        }

        #endregion

        #region Methods
        private void AIBaseClientBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if(args.Buff.Name == "VarusQLaunch" && (new[] { OrbwalkerMode.Combo, OrbwalkerMode.Harass }).Contains(Orbwalker.ActiveMode))
            {
                //ChargingStart = LastCastPacketSentEntry.Tick;
                ChargingStart = Game.Time;
                spells[Spells.W].Cast();
            }
        }

        private void AIBaseClientBuffLose(AIBaseClient sender, AIBaseClientBuffLoseEventArgs args)
        {
            if (args.Buff.Name == "VarusQLaunch" && (new[] { OrbwalkerMode.Combo, OrbwalkerMode.Harass }).Contains(Orbwalker.ActiveMode))
            {
                spells[Spells.Q].Range = 925;
            }
        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(
                (spells[Spells.Q].ChargedMaxRange + spells[Spells.Q].Width) * 1.1f);
            if (target == null)
            {
                return;
            }

            Items(target);

            if (spells[Spells.E].IsReady() && !spells[Spells.Q].IsCharging
                && Menu["Combo"].GetValue<MenuBool>("ElVarus.Combo.E"))
            {
                if (IsKillable(spells[Spells.E], target) || GetWStacks(target) >= Menu["Combo"].GetValue<MenuSlider>("ElVarus.Combo.Stack.Count").Value && CanCastQE)
                {
                    var prediction = spells[Spells.E].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh)
                    {
                        spells[Spells.E].Cast(prediction.CastPosition);
                        CanCastQE = false;
                        //DelayAction.Add((int)(prediction.CastPosition.Distance(Player) * 1000 / spells[Spells.E].Speed + Game.Ping *5), () => { CanCastQE = true; });
                        return;
                    }
                }
            }

            if (spells[Spells.Q].IsReady() && Menu["Combo"].GetValue<MenuBool>("ElVarus.Combo.Q") && CanCastQE)
            {
                if (spells[Spells.Q].IsCharging || Menu["Combo"].GetValue<MenuBool>("ElVarus.combo.always.Q")
                    || target.Distance(Player) > Player.GetRealAutoAttackRange() * 1.2f
                    || GetWStacks(target) >= Menu["Combo"].GetValue<MenuSlider>("ElVarus.Combo.Stack.Count").Value
                    || IsKillable(spells[Spells.Q], target))
                {
                    if (!spells[Spells.Q].IsCharging)
                    {
                        spells[Spells.Q].StartCharging();
                    }

                    if (spells[Spells.Q].IsCharging)
                    {
                        var prediction = spells[Spells.Q].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.VeryHigh && spells[Spells.Q].IsInRange(target))
                        {
                            spells[Spells.Q].ShootChargedSpell(prediction.CastPosition);
                            CanCastQE = false;
                            //DelayAction.Add((int)(prediction.CastPosition.Distance(Player) * 1000 / spells[Spells.Q].Speed + Game.Ping *5 ), () => { CanCastQE = true; });
                            return;
                        }
                    }
                }
            }

            if (spells[Spells.R].IsReady() && !spells[Spells.Q].IsCharging
                && target.IsValidTarget(spells[Spells.R].Range) && Menu["Combo"].GetValue<MenuBool>("ElVarus.Combo.R"))
            {
                var pred = spells[Spells.R].GetPrediction(target);
                if (pred.Hitchance >= HitChance.VeryHigh)
                {
                    var ultimateHits = GameObjects.EnemyHeroes.Where(x => x.Distance(target) <= 450f).ToList();
                    if (ultimateHits.Count >= Menu["Combo"].GetValue<MenuSlider>("ElVarus.Combo.R.Count").Value)
                    {
                        spells[Spells.R].Cast(pred.CastPosition);
                    }
                }
            }
        }

        private static int GetWStacks(AIBaseClient target)
        {
            return target.GetBuffCount("varuswdebuff");
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].ChargedMaxRange);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (Player.ManaPercent > Menu["Harass"].GetValue<MenuSlider>("minmanaharass").Value)
            {
                if (Menu["Harass"].GetValue<MenuBool>("ElVarus.Harass.E") && spells[Spells.E].IsReady() && GetWStacks(target) >= 1)
                {
                    var prediction = spells[Spells.E].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh)
                    {
                        spells[Spells.E].Cast(prediction.CastPosition);
                    }
                }

                if (Menu["Harass"].GetValue<MenuBool>("ElVarus.Harass.Q") && spells[Spells.Q].IsReady())
                {
                    if (!spells[Spells.Q].IsCharging)
                    {
                        spells[Spells.Q].StartCharging();
                    }

                    if (spells[Spells.Q].IsCharging)
                    {
                        var prediction = spells[Spells.Q].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.VeryHigh && spells[Spells.Q].IsInRange(target))
                        {
                            spells[Spells.Q].ShootChargedSpell(prediction.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Items(AIBaseClient target)
        {
            var botrk = new Items.Item(ItemId.Blade_of_the_Ruined_King, 550);
            var ghost = new Items.Item(ItemId.Youmuus_Ghostblade, 0);
            var cutlass = new Items.Item(ItemId.Bilgewater_Cutlass, 550);

            var useYoumuu = Menu["Items"].GetValue<MenuBool>("ElVarus.Items.Youmuu");
            var useCutlass = Menu["Items"].GetValue<MenuBool>("ElVarus.Items.Cutlass");
            var useBlade = Menu["Items"].GetValue<MenuBool>("ElVarus.Items.Blade");

            var useBladeEhp = Menu["Items"].GetValue<MenuSlider>("ElVarus.Items.Blade.EnemyEHP").Value;
            var useBladeMhp = Menu["Items"].GetValue<MenuSlider>("ElVarus.Items.Blade.EnemyMHP").Value;

            if (botrk.IsReady && botrk.IsOwned() && botrk.IsInRange(target)
                && target.HealthPercent <= useBladeEhp && useBlade)
            {
                botrk.Cast(target);
            }

            if (botrk.IsReady && botrk.IsOwned() && botrk.IsInRange(target)
                && Player.HealthPercent <= useBladeMhp && useBlade)
            {
                botrk.Cast(target);
            }

            if (cutlass.IsReady && cutlass.IsOwned() && cutlass.IsInRange(target)
                && target.HealthPercent <= useBladeEhp && useCutlass)
            {
                cutlass.Cast(target);
            }

            if (ghost.IsReady && ghost.IsOwned() && target.IsValidTarget(spells[Spells.Q].Range) && useYoumuu)
            {
                ghost.Cast();
            }
        }

        private static void JungleClear()
        {
            var useQ = Menu["Clear"].GetValue<MenuBool>("useQFarmJungle");
            var useE = Menu["Clear"].GetValue<MenuBool>("useEFarmJungle");
            var minmana = Menu["Clear"].GetValue<MenuSlider>("minmanaclear").Value;
            var minions = MinionManager.GetMinions(
                Player.Position,
                700,
                SPrediction.MinionManager.MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (Player.ManaPercent >= minmana)
            {
                foreach (var minion in minions)
                {
                    if (spells[Spells.Q].IsReady() && useQ)
                    {
                        if (!spells[Spells.Q].IsCharging)
                        {
                            spells[Spells.Q].StartCharging();
                        }

                        if (spells[Spells.Q].IsCharging && spells[Spells.Q].Range >= spells[Spells.Q].ChargedMaxRange)
                        {
                            spells[Spells.Q].ShootChargedSpell(minion.Position);
                        }
                    }

                    if (spells[Spells.E].IsReady() && useE)
                    {
                        spells[Spells.E].CastOnUnit(minion);
                    }
                }
            }
        }

        public static bool IsKillable(Spell s, AIHeroClient target)
        {
            return Damage.GetSpellDamage(Player, target, Player.GetSpellSlot(s.Name), DamageStage.Default) >= target.Health;
        }
        //Credits to God :cat_lazy:
        private static void Killsteal()
        {
            if (Menu["Misc"].GetValue<MenuBool>("ElVarus.KSSS") && spells[Spells.Q].IsReady())
            {
                foreach (var target in
                    GameObjects.EnemyHeroes.Where(
                        enemy =>
                        enemy.IsValidTarget() && IsKillable(spells[Spells.Q], enemy)
                        && Player.Distance(enemy.Position) <= spells[Spells.Q].ChargedMaxRange))
                {
                    if (!spells[Spells.Q].IsCharging)
                    {
                        spells[Spells.Q].StartCharging();
                    }

                    if (spells[Spells.Q].IsCharging)
                    {
                        var prediction = spells[Spells.Q].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.VeryHigh)
                        {
                            spells[Spells.Q].ShootChargedSpell(prediction.CastPosition);
                        }
                    }
                }
            }
        }

        private static void LaneClear()
        {
            if (Player.ManaPercent < Menu["Clear"].GetValue<MenuSlider>("minmanaclear").Value)
            {
                return;
            }

            var minions = MinionManager.GetMinions(Player.Position, spells[Spells.Q].Range);
            if (minions.Count <= 0)
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && Menu["Clear"].GetValue<MenuBool>("useQFarm"))
            {
                var allMinions = MinionManager.GetMinions(Player.Position, spells[Spells.Q].Range);
                {
                    foreach (var minion in
                        allMinions.Where(minion => minion.Health <= Player.GetSpellDamage(minion, SpellSlot.Q)))
                    {
                        var killcount = 0;

                        foreach (var colminion in minions)
                        {
                            if (colminion.Health <= spells[Spells.Q].GetDamage(colminion))
                            {
                                killcount++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (killcount >= Menu["Clear"].GetValue<MenuSlider>("ElVarus.Count.Minions").Value)
                        {
                            if (spells[Spells.Q].IsInRange(minion))
                            {
                                spells[Spells.Q].ShootChargedSpell(minion.Position);
                                return;
                            }
                        }
                    }
                }
            }

            if (!Menu["Clear"].GetValue<MenuBool>("useQFarm") || !spells[Spells.E].IsReady())
            {
                return;
            }

            var minionkillcount =
                minions.Count(x => spells[Spells.E].CanCast(x) && x.Health <= spells[Spells.E].GetDamage(x));

            if (minionkillcount >= Menu["Clear"].GetValue<MenuSlider>("ElVarus.Count.Minions.E").Value)
            {
                foreach (var minion in minions.Where(x => x.Health <= spells[Spells.E].GetDamage(x)))
                {
                    spells[Spells.E].Cast(minion);
                }
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (spells[Spells.Q].IsCharging)
            {
                // (1600-925)/2000 = 0.3375
                spells[Spells.Q].Range = 800 + (int)((Game.Time - ChargingStart) * 1000 * 0.3375f);
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
            }

            Killsteal();

            var target = TargetSelector.GetTarget(spells[Spells.R].Range);
            if (spells[Spells.R].IsReady() && target.IsValidTarget()
                && Menu["Combo"].GetValue<MenuKeyBind>("ElVarus.SemiR").Active)
            {
                spells[Spells.R].CastOnUnit(target);
            }
        }
        public static void Drawing_OnDraw(EventArgs args)
        {
            var drawOff = Menu["Misc"].GetValue<MenuBool>("ElVarus.Draw.off");
            var drawQ = Menu["Misc"].GetValue<MenuBool>("ElVarus.Draw.Q");
            var drawW = Menu["Misc"].GetValue<MenuBool>("ElVarus.Draw.W");
            var drawE = Menu["Misc"].GetValue<MenuBool>("ElVarus.Draw.E");
            var drawR = Menu["Misc"].GetValue<MenuBool>("ElVarus.Draw.E");

            if (drawOff)
            {
                return;
            }

            if (drawQ)
            {
                if (Varus.spells[Spells.Q].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Varus.spells[Spells.Q].Range,
                        Varus.spells[Spells.Q].IsReady() ? Color.Green : Color.Red);
                }
            }

            if (drawW)
            {
                if (Varus.spells[Spells.W].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Varus.spells[Spells.W].Range,
                        Varus.spells[Spells.W].IsReady() ? Color.Green : Color.Red);
                }
            }

            if (drawE)
            {
                if (Varus.spells[Spells.E].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Varus.spells[Spells.E].Range,
                        Varus.spells[Spells.E].IsReady() ? Color.Green : Color.Red);
                }
            }

            if (drawR)
            {
                if (Varus.spells[Spells.R].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Varus.spells[Spells.R].Range,
                        Varus.spells[Spells.R].IsReady() ? Color.Green : Color.Red);
                }
            }
        }

        #endregion
    }
}
