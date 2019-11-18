using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;

namespace DaoHungAIO.Champions
{
    public enum Spells
    {
        Q,

        W,

        E,

        R
    }
    class Rengar : Standards
    {
        #region Properties

        private static IEnumerable<AIHeroClient> Enemies => HeroManager.Enemies;

        #endregion

        #region Public Methods and Operators


        public Rengar()
        {
            try
            {

                Ignite = Player.GetSpellSlot("summonerdot");

                spells[Spells.E].SetSkillshot(0.25f, 70f, 1500f, true, SkillshotType.SkillshotLine);

                MenuInit.Initialize();
                Game.OnUpdate += OnUpdate;
                Drawing.OnDraw += OnDraw;
                CustomEvents.Unit.OnDash += OnDash;
                Drawing.OnEndScene += OnDrawEndScene;
                AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
                Orbwalking.AfterAttack += AfterAttack;
                Orbwalking.BeforeAttack += BeforeAttack;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Methods

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var targ = args.Target as AIHeroClient;
            if (!args.Unit.IsMe || targ == null)
            {
                return;
            }

            if (!spells[Spells.Q].IsReady() || !spells[Spells.Q].IsReady() || !Orbwalker.ActiveMode.Equals(Orbwalking.OrbwalkingMode.Combo))
            {
                return;
            }

            if (targ.Distance(Player) <= Orbwalking.GetRealAutoAttackRange(Player) - 10)
            {
                if (IsActive("Combo.Use.items"))
                {
                    ActiveModes.CastItems(targ);
                }
                spells[Spells.Q].Cast();
            }
        }


        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var targ = target as AIBaseClient;
            if (!unit.IsMe || targ == null)
            {
                return;
            }

            Orbwalker.SetOrbwalkingPoint(Vector3.Zero);
            var mode = Orbwalker.ActiveMode;
            if (mode.Equals(Orbwalking.OrbwalkingMode.None) || mode.Equals(Orbwalking.OrbwalkingMode.LastHit) || mode.Equals(Orbwalking.OrbwalkingMode.LaneClear))
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && spells[Spells.Q].Cast())
            {
                return;
            }

            if (IsActive("Combo.Use.items"))
            {
                ActiveModes.CastItems(targ);
            }
        }

        private static void Heal()
        {
            if (RengarR || Ferocity != 4 || Player.InFountain() || Player.Buffs.Any(b => b.Name.ToLower().Contains("recall") || b.Name.ToLower().Contains("teleport")))
            {
                return;
            }

            if (Player.CountEnemiesInRange(1000) >= 1 && spells[Spells.W].IsReady())
            {
                if (IsActive("Heal.AutoHeal")
                    && (Player.Health / Player.MaxHealth) * 100
                    <= MenuInit.Menu.Item("Heal.HP").GetValue<Slider>().Value)
                {
                    spells[Spells.W].Cast();
                }
            }
        }

        private static void KillstealHandler()
        {
            if (RengarR)
            {
                return;
            }

            if (!IsActive("Killsteal.On") || Player.IsRecalling())
            {
                return;
            }

            var target = Enemies.FirstOrDefault(x => x.IsValidTarget(spells[Spells.E].Range));
            if (target == null)
            {
                return;
            }

            if (IsActive("Killsteal.Use.W") && spells[Spells.W].GetDamage(target) > target.Health && target.IsValidTarget(spells[Spells.W].Range))
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("Killsteal.Use.E") && spells[Spells.E].GetDamage(target) > target.Health && target.IsValidTarget(spells[Spells.E].Range))
            {
                var prediction = spells[Spells.E].GetPrediction(target);
                if (prediction.Hitchance >= HitChance.VeryHigh)
                {
                    spells[Spells.E].Cast(prediction.CastPosition);
                }
            }
        }

        private static void OnDash(AIBaseClient sender, Dash.DashItem args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            var target = TargetSelector.GetTarget(1500f, TargetSelector.DamageType.Physical);
            if (!target.IsValidTarget())
            {
                return;
            }


            var mode = Orbwalker.ActiveMode;
            if (!mode.Equals(Orbwalking.OrbwalkingMode.Combo) || !mode.Equals(Orbwalking.OrbwalkingMode.Mixed))
            {
                return;
            }

            Orbwalker.SetOrbwalkingPoint(Vector3.Zero);
            if (Ferocity == 4)
            {
                switch (IsListActive("Combo.Prio").SelectedIndex)
                {
                    case 0:
                        if (spells[Spells.E].IsReady())
                        {
                            var targetE = TargetSelector.GetTarget(
                                spells[Spells.E].Range,
                                TargetSelector.DamageType.Physical);

                            if (targetE.IsValidTarget())
                            {
                                var pred = spells[Spells.E].GetPrediction(targetE);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    Utility.DelayAction.Add(300, () => spells[Spells.E].Cast(target));
                                }
                            }
                        }
                        break;
                    case 2:
                        if (spells[Spells.Q].IsReady()
                            && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            spells[Spells.Q].Cast();
                        }
                        break;
                }
            }
            else
            {
                if (IsListActive("Combo.Prio").SelectedIndex != 0)
                {
                    if (spells[Spells.E].IsReady())
                    {
                        var targetE = TargetSelector.GetTarget(
                            spells[Spells.E].Range,
                            TargetSelector.DamageType.Physical);
                        if (targetE.IsValidTarget(spells[Spells.E].Range))
                        {
                            var pred = spells[Spells.E].GetPrediction(targetE);
                            if (pred.Hitchance >= HitChance.VeryHigh)
                            {
                                Utility.DelayAction.Add(300, () => spells[Spells.E].Cast(target));
                            }
                        }
                    }
                }
            }

            switch (IsListActive("Combo.Prio").SelectedIndex)
            {
                case 0:
                    if (spells[Spells.E].IsReady() && target.IsValidTarget(spells[Spells.E].Range))
                    {
                        var pred = spells[Spells.E].GetPrediction(target);
                        Utility.DelayAction.Add(300, () => spells[Spells.E].Cast(pred.CastPosition));
                    }
                    break;

                case 2:
                    if (IsActive("Beta.Cast.Q1") && RengarR)
                    {
                        spells[Spells.Q].Cast();
                    }
                    break;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            try
            {
                var drawW = MenuInit.Menu.Item("Misc.Drawings.W").GetValue<Circle>();
                var drawE = MenuInit.Menu.Item("Misc.Drawings.E").GetValue<Circle>();
                var drawExclamation = MenuInit.Menu.Item("Misc.Drawings.Exclamation").GetValue<Circle>();
                var drawSearchRange = MenuInit.Menu.Item("Beta.Search.Range").GetValue<Circle>();
                var searchrange = MenuInit.Menu.Item("Beta.searchrange").GetValue<Slider>().Value;
                var drawsearchrangeQ = MenuInit.Menu.Item("Beta.Search.QCastRange").GetValue<Circle>();
                var searchrangeQCastRange = MenuInit.Menu.Item("Beta.searchrange.Q").GetValue<Slider>().Value;

                if (IsActive("Misc.Drawings.Off"))
                {
                    return;
                }

                if (IsActive("Beta.Cast.Q1"))
                {
                    if (drawSearchRange.Active && spells[Spells.R].Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, searchrange, Color.Orange);
                    }

                    if (drawsearchrangeQ.Active && spells[Spells.R].Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, searchrangeQCastRange, Color.Orange);
                    }
                }

                if (RengarR && drawExclamation.Active)
                {
                    if (spells[Spells.R].Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, 1450f, Color.DeepSkyBlue);
                    }
                }

                if (drawW.Active)
                {
                    if (spells[Spells.W].Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.W].Range, Color.Purple);
                    }
                }

                if (drawE.Active)
                {
                    if (spells[Spells.E].Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.E].Range, Color.White);
                    }
                }

                if (IsActive("Misc.Drawings.Prioritized"))
                {
                    switch (IsListActive("Combo.Prio").SelectedIndex)
                    {
                        case 0:
                            Drawing.DrawText(
                                Drawing.Width * 0.70f,
                                Drawing.Height * 0.95f,
                                Color.Yellow,
                                "Prioritized spell: E");
                            break;
                        case 1:
                            Drawing.DrawText(
                                Drawing.Width * 0.70f,
                                Drawing.Height * 0.95f,
                                Color.White,
                                "Prioritized spell: W");
                            break;
                        case 2:
                            Drawing.DrawText(
                                Drawing.Width * 0.70f,
                                Drawing.Height * 0.95f,
                                Color.White,
                                "Prioritized spell: Q");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OnDrawEndScene(EventArgs args)
        {
            try
            {
                if (IsActive("Misc.Drawings.Minimap") && spells[Spells.R].Level > 0)
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, spells[Spells.R].Range, Color.White, 1, 23, true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OnProcessSpellCast(
    AIBaseClient sender,
    AIBaseClientProcessSpellCastEventArgs args
)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (args.SData.Name.Equals("rengarr", StringComparison.InvariantCultureIgnoreCase))
            {
                if (ActiveModes.Youmuu.IsOwned() && ActiveModes.Youmuu.IsReady())
                {
                    Utility.DelayAction.Add(2500, () => ActiveModes.Youmuu.Cast());
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    ActiveModes.Combo();
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    ActiveModes.Laneclear();
                    ActiveModes.Jungleclear();
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    ActiveModes.Harass();
                    break;
            }

            SwitchCombo();
            Heal();
            KillstealHandler();

            // E on Immobile targets
            if (IsActive("Misc.Root") && spells[Spells.E].IsReady())
            {
                if (!RengarR)
                {
                    var target = HeroManager.Enemies.FirstOrDefault(h => h.IsValidTarget(spells[Spells.E].Range));
                    if (target != null)
                    {
                        if (Ferocity == 4)
                        {
                            spells[Spells.E].CastIfHitchanceEquals(target, HitChance.Immobile);
                        }
                    }
                }
            }

            if (IsActive("Beta.Cast.Q1") && IsListActive("Combo.Prio").SelectedIndex == 2)
            {
                if (Ferocity != 4)
                {
                    return;
                }

                var searchrange = MenuInit.Menu.Item("Beta.searchrange").GetValue<Slider>().Value;
                var target = HeroManager.Enemies.FirstOrDefault(h => h.IsValidTarget(searchrange, false));
                if (!target.IsValidTarget())
                {
                    return;
                }

                // Check if Rengar is in ultimate
                if (RengarR)
                {
                    // Check if the player distance <= than the set search range
                    if (Player.Distance(target) <= MenuInit.Menu.Item("Beta.searchrange.Q").GetValue<Slider>().Value)
                    {
                        // Cast Q with the set delay
                        Utility.DelayAction.Add(
                            MenuInit.Menu.Item("Beta.Cast.Q1.Delay").GetValue<Slider>().Value,
                            () => spells[Spells.Q].Cast());
                    }
                }
            }

            spells[Spells.R].Range = 1000 + spells[Spells.R].Level * 1000;
        }

        private static void SwitchCombo()
        {
            try
            {
                var switchTime = Utils.GameTimeTickCount - LastSwitch;
                if (MenuInit.Menu.Item("Combo.Switch").GetValue<KeyBind>().Active && switchTime >= 350)
                {
                    switch (IsListActive("Combo.Prio").SelectedIndex)
                    {
                        case 0:
                            MenuInit.Menu.Item("Combo.Prio").SetValue(new StringList(new[] { "E", "W", "Q" }, 2));
                            LastSwitch = Utils.GameTimeTickCount;
                            break;
                        case 1:
                            MenuInit.Menu.Item("Combo.Prio").SetValue(new StringList(new[] { "E", "W", "Q" }, 0));
                            LastSwitch = Utils.GameTimeTickCount;
                            break;

                        default:
                            MenuInit.Menu.Item("Combo.Prio").SetValue(new StringList(new[] { "E", "W", "Q" }, 0));
                            LastSwitch = Utils.GameTimeTickCount;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion
    }
    internal class ActiveModes : Standards
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Handles combo
        /// </summary>
        public static void Combo()
        {
            var target = TargetSelector.SelectedTarget
                             ?? TargetSelector.GetTarget(spells[Spells.E].Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget() == false)
            {
                return;
            }

            if (TargetSelector.SelectedTarget != null)
            {
                Orbwalker.ForceTarget(target);
            }

            #region RengarR

            if (Ferocity <= 3)
            {
                if (spells[Spells.Q].IsReady() && IsActive("Combo.Use.Q")
                    && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                {
                    spells[Spells.Q].Cast();
                }

                if (!RengarR)
                {
                    if (!HasPassive)
                    {
                        if (spells[Spells.E].IsReady() && IsActive("Combo.Use.E"))
                        {
                            CastE(target);
                        }
                    }
                    else
                    {
                        if (spells[Spells.E].IsReady() && IsActive("Combo.Use.E"))
                        {
                            if (Player.IsDashing())
                            {
                                CastE(target);
                            }
                        }
                    }
                }

                if (spells[Spells.W].IsReady() && IsActive("Combo.Use.W"))
                {
                    CastW();
                }
            }

            if (Ferocity == 4)
            {
                switch (IsListActive("Combo.Prio").SelectedIndex)
                {
                    case 0:
                        if (!RengarR)
                        {
                            if (spells[Spells.E].IsReady() && !HasPassive)
                            {
                                CastE(target);

                                if (IsActive("Combo.Switch.E") && Utils.GameTimeTickCount - LastSwitch >= 350)
                                {
                                    MenuInit.Menu.Item("Combo.Prio")
                                        .SetValue(new StringList(new[] { "E", "W", "Q" }, 2));
                                    LastSwitch = Utils.GameTimeTickCount;
                                }
                            }
                        }
                        else
                        {
                            if (spells[Spells.E].IsReady() && IsActive("Combo.Use.E"))
                            {
                                if (Player.IsDashing())
                                {
                                    CastE(target);
                                }
                            }
                        }
                        break;
                    case 1:
                        if (IsActive("Combo.Use.W") && spells[Spells.W].IsReady())
                        {
                            CastW();
                        }
                        break;
                    case 2:
                        if (spells[Spells.Q].IsReady() && IsActive("Combo.Use.Q")
                            && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            spells[Spells.Q].Cast();
                        }
                        break;
                }
            }

            #region Summoner spells

            if (Youmuu.IsReady() && Youmuu.IsOwned() && target.IsValidTarget(spells[Spells.Q].Range))
            {
                Youmuu.Cast();
            }

            if (IsActive("Combo.Use.Ignite") && target.IsValidTarget(600f) && IgniteDamage(target) >= target.Health)
            {
                Player.Spellbook.CastSpell(Ignite, target);
            }

            #endregion
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        ///     Handles E cast
        /// </summary>
        /// <param name="target"></param>
        private static void CastE(AIBaseClient target)
        {
            if (!spells[Spells.E].IsReady() || !target.IsValidTarget(spells[Spells.E].Range))
            {
                return;
            }

            var pred = spells[Spells.E].GetPrediction(target);
            if (pred.Hitchance >= HitChance.High)
            {
                spells[Spells.E].Cast(target);
            }
        }

        /// <summary>
        ///     Handles W casting
        /// </summary>
        private static void CastW()
        {
            if (!spells[Spells.W].IsReady())
            {
                return;
            }

            if (GetWHits().Item1 > 0)
            {
                spells[Spells.W].Cast();
            }
        }

        /// <summary>
        ///     Get W hits
        /// </summary>
        /// <returns></returns>
        private static Tuple<int, List<AIHeroClient>> GetWHits()
        {
            try
            {
                var hits =
                    HeroManager.Enemies.Where(
                        e =>
                        e.IsValidTarget() && e.Distance(Player) < 450f
                        || e.Distance(Player) < 450f).ToList();

                return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return new Tuple<int, List<AIHeroClient>>(0, null);
        }

        #endregion

        /// <summary>
        ///     Harass
        /// </summary>
        public static void Harass()
        {
            // ReSharper disable once ConvertConditionalTernaryToNullCoalescing
            var target = TargetSelector.SelectedTarget != null
                             ? TargetSelector.SelectedTarget
                             : TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget() == false)
            {
                return;
            }

            #region RengarR

            if (Ferocity == 4)
            {
                switch (IsListActive("Harass.Prio").SelectedIndex)
                {
                    case 0:
                        if (!HasPassive && IsActive("Harass.Use.E") && spells[Spells.E].IsReady())
                        {
                            CastE(target);
                        }
                        break;

                    case 1:
                        if (IsActive("Harass.Use.Q") && target.IsValidTarget(spells[Spells.Q].Range))
                        {
                            spells[Spells.Q].Cast();
                        }
                        break;
                }
            }

            if (Ferocity <= 3)
            {
                if (IsActive("Harass.Use.Q") && target.IsValidTarget(spells[Spells.Q].Range))
                {
                    spells[Spells.Q].Cast();
                }

                if (!RengarR)
                {
                    if (!HasPassive && IsActive("Harass.Use.E") && spells[Spells.E].IsReady())
                    {
                        CastE(target);
                    }

                    if (IsActive("Harass.Use.W"))
                    {
                        CastW();
                    }
                }
            }
        }

        /// <summary>
        ///     Jungle clear
        /// </summary>
        public static void Jungleclear()
        {
            var minion =
          MinionManager.GetMinions(
              Player.Position,
              spells[Spells.W].Range,
              MinionTypes.All,
              MinionTeam.Neutral,
              MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (minion == null)
            {
                return;
            }

            CastItems(minion);

            if (Ferocity == 4 && IsActive("Jungle.Save.Ferocity"))
            {
                if (minion.IsValidTarget(spells[Spells.W].Range) && !HasPassive)
                {
                    LaneItems(minion);
                }
                return;
            }


            if (IsActive("Jungle.Use.Q") && spells[Spells.Q].IsReady()
                && minion.IsValidTarget(spells[Spells.Q].Range + 100))
            {
                spells[Spells.Q].Cast();
            }

            LaneItems(minion);

            if (Ferocity == 4 && (Player.Health / Player.MaxHealth) * 100 <= 20)
            {
                spells[Spells.W].Cast();
            }

            if (!HasPassive)
            {
                if (IsActive("Jungle.Use.W") && spells[Spells.W].IsReady()
                    && minion.IsValidTarget(spells[Spells.W].Range))
                {
                    if (Ferocity == 4 && spells[Spells.Q].IsReady())
                    {
                        return;
                    }
                    spells[Spells.W].Cast();
                }
            }

            if (IsActive("Jungle.Use.E") && spells[Spells.E].IsReady()
                && minion.IsValidTarget(spells[Spells.E].Range) && Ferocity != 4)
            {
                spells[Spells.E].Cast(minion);
            }
        }

        /// <summary>
        ///     Lane clear
        /// </summary>
        public static void Laneclear()
        {
            var minion = MinionManager.GetMinions(Player.Position, spells[Spells.W].Range).FirstOrDefault();
            if (minion == null)
            {
                return;
            }

            if (Player.Spellbook.IsAutoAttack || Player.IsWindingUp)
            {
                return;
            }

            if (Ferocity == 4 && IsActive("Clear.Save.Ferocity"))
            {
                if (minion.IsValidTarget(spells[Spells.W].Range))
                {
                    LaneItems(minion);
                }
                return;
            }

            if (IsActive("Clear.Use.Q") && spells[Spells.Q].IsReady()
                && minion.IsValidTarget(spells[Spells.Q].Range))
            {
                spells[Spells.Q].Cast();
            }

            LaneItems(minion);

            if (IsActive("Clear.Use.W") && spells[Spells.W].IsReady()
                && minion.IsValidTarget(spells[Spells.W].Range))
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("Clear.Use.E") && spells[Spells.E].IsReady()
                && minion.IsValidTarget(spells[Spells.E].Range))
            {
                if (Ferocity == 4)
                {
                    return;
                }

                spells[Spells.E].Cast(minion.Position);
            }
        }

        /// <summary>
        ///     Gets Youmuus Ghostblade
        /// </summary>
        /// <value>
        ///     Youmuus Ghostblade
        /// </value>
        public static Items.Item Youmuu => new Items.Item((int)ItemId.Youmuus_Ghostblade);

        /// <summary>
        ///     Gets Ravenous Hydra
        /// </summary>
        /// <value>
        ///     Ravenous Hydra
        /// </value>
        private static Items.Item Hydra => new Items.Item((int)ItemId.Ravenous_Hydra_Melee_Only, 400);

        /// <summary>
        ///     Gets Tiamat Item
        /// </summary>
        /// <value>
        ///     Tiamat Item
        /// </value>
        private static Items.Item Tiamat => new Items.Item((int)ItemId.Tiamat_Melee_Only, 400);

        /// <summary>
        ///     Gets Titanic Hydra
        /// </summary>
        /// <value>
        ///     Titanic Hydra
        /// </value>
        private static Items.Item Titanic => new Items.Item((int)ItemId.Titanic_Hydra);

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool LaneItems(AIBaseClient target)
        {
            var units =
                MinionManager.GetMinions(385, MinionTypes.All, MinionTeam.NotAlly).Count(o => !(o is AITurretClient));
            var count = units;
            var tiamat = Tiamat;
            if (tiamat.IsReady() && count > 0 && tiamat.Cast())
            {
                return true;
            }

            var hydra = Hydra;
            if (Hydra.IsReady() && count > 0 && hydra.Cast())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Cast items
        /// </summary>
        /// <param name="target"></param>
        /// <returns>true or false</returns>
        public static bool CastItems(AIBaseClient target)
        {
            if (Player.IsDashing() || Player.IsWindingUp || RengarR)
            {
                return false;
            }

            var heroes = Player.GetEnemiesInRange(385).Count;
            var count = heroes;

            var tiamat = Tiamat;
            if (tiamat.IsReady() && count > 0 && tiamat.Cast())
            {
                return true;
            }

            var hydra = Hydra;
            if (Hydra.IsReady() && count > 0 && hydra.Cast())
            {
                return true;
            }

            var youmuus = Youmuu;
            if (Youmuu.IsReady() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo
                || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && youmuus.Cast())
            {
                return true;
            }

            var titanic = Titanic;
            return titanic.IsReady() && count > 0 && titanic.Cast();
        }

        #endregion
    }

    public class MenuInit
    {
        #region Static Fields

        public static Menu Menu;

        #endregion

        #region Public Methods and Operators

        public static void Initialize()
        {
            Menu = new Menu("DH.Rengar credit JQuery", "ElRengar", true);
            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            Standards.Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);

            TargetSelector.AddToMenu(TargetSelectorMenu());

            var comboMenu = Menu.AddSubMenu(new Menu("Modes", "Modes"));
            {
                comboMenu.SubMenu("Summoner spells")
                    .AddItem(new MenuItem("Combo.Use.Ignite", "Use Ignite").SetValue(true));

                comboMenu.SubMenu("Combo").AddItem(new MenuItem("Combo.Use.items", "Use Items").SetValue(true));
                comboMenu.SubMenu("Combo").AddItem(new MenuItem("Combo.Use.Q", "Use Q").SetValue(true));
                comboMenu.SubMenu("Combo").AddItem(new MenuItem("Combo.Use.W", "Use W").SetValue(true));
                comboMenu.SubMenu("Combo").AddItem(new MenuItem("Combo.Use.E", "Use E").SetValue(true));
                comboMenu.SubMenu("Combo")
                    .AddItem(new MenuItem("Combo.Switch.E", "Switch E prio to Q after E cast").SetValue(true));
                comboMenu.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("Combo.Prio", "Prioritize").SetValue(new StringList(new[] { "E", "W", "Q" }, 2)));
                comboMenu.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("Combo.Switch", "Switch priority").SetValue(
                            new KeyBind("L".ToCharArray()[0], KeyBindType.Press)));

                comboMenu.SubMenu("Combo").AddItem(new MenuItem("Combo.Use.QQ", "4 ferocity Q reset").SetValue(true));


                comboMenu.SubMenu("Harass").AddItem(new MenuItem("Harass.Use.Q", "Use Q").SetValue(true));
                comboMenu.SubMenu("Harass").AddItem(new MenuItem("Harass.Use.W", "Use W").SetValue(true));
                comboMenu.SubMenu("Harass").AddItem(new MenuItem("Harass.Use.E", "Use E").SetValue(true));
                comboMenu.SubMenu("Harass")
                    .AddItem(new MenuItem("Harass.Prio", "Prioritize").SetValue(new StringList(new[] { "E", "Q" }, 1)));
            }

            var clearMenu = Menu.AddSubMenu(new Menu("Clear", "clear"));
            {
                clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("Clear.Use.Q", "Use Q").SetValue(true));
                clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("Clear.Use.W", "Use W").SetValue(true));
                clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("Clear.Use.E", "Use E").SetValue(true));
                clearMenu.SubMenu("Laneclear")
                    .AddItem(new MenuItem("Clear.Save.Ferocity", "Save ferocity").SetValue(false));

                clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("Jungle.Use.Q", "Use Q").SetValue(true));
                clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("Jungle.Use.W", "Use W").SetValue(true));
                clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("Jungle.Use.E", "Use E").SetValue(true));
                clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("Jungle.Save.Ferocity", "Save ferocity").SetValue(false));
            }

            var healMenu = Menu.AddSubMenu(new Menu("Heal", "heal"));
            {
                healMenu.AddItem(new MenuItem("Heal.AutoHeal", "Auto heal yourself").SetValue(true));
                healMenu.AddItem(new MenuItem("Heal.HP", "Self heal at >= ").SetValue(new Slider(25, 1, 100)));
            }

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", "Killsteal"));
            {
                killstealMenu.AddItem(new MenuItem("Killsteal.On", "Active").SetValue(true));
                killstealMenu.AddItem(new MenuItem("Killsteal.Use.W", "Use W").SetValue(true));
                killstealMenu.AddItem(new MenuItem("Killsteal.Use.E", "Use E").SetValue(true));
            }

            var betaMenu = Menu.AddSubMenu(new Menu("Beta options", "BetaOptions"));
            {
                betaMenu.AddItem(new MenuItem("Beta.Cast.Q1", "Use beta Q").SetValue(true));
                betaMenu.AddItem(
                    new MenuItem("Beta.Cast.Q1.Delay", "Cast Q delay").SetValue(new Slider(300, 100, 2000)));
                betaMenu.AddItem(new MenuItem("Assassin.searchrange", "Assassin search range"));

                betaMenu.AddItem(
                    new MenuItem("Beta.searchrange", "Search range").SetValue(new Slider(1500, 1000, 2500)));

                betaMenu.AddItem(
                    new MenuItem("Beta.searchrange.Q", "Q cast range").SetValue(new Slider(600, 500, 1500)));

                betaMenu.AddItem(new MenuItem("Beta.Search.Range", "Draw search range").SetValue(new Circle()));
                betaMenu.AddItem(new MenuItem("Beta.Search.QCastRange", "Draw Q cast range").SetValue(new Circle()));
            }

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            {
                miscMenu.AddItem(new MenuItem("Misc.Drawings.Off", "Turn drawings off").SetValue(false));
                miscMenu.AddItem(
                    new MenuItem("Misc.Drawings.Exclamation", "Draw exclamation mark range").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("Misc.Drawings.Prioritized", "Draw Prioritized").SetValue(true));
                miscMenu.AddItem(new MenuItem("Misc.Drawings.W", "Draw W").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("Misc.Drawings.E", "Draw E").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("Misc.Drawings.Minimap", "Draw R on minimap").SetValue(true));

                miscMenu.AddItem(new MenuItem("Misc.Root", "Auto E on stunned targets").SetValue(false));
            }

            Menu.AddItem(new MenuItem("sep1", ""));
            Menu.AddItem(new MenuItem("sep2", $"Version: {Standards.ScriptVersion}"));
            Menu.AddItem(new MenuItem("sep3", "Made By jQuery"));

        }

        #endregion

        #region Methods

        private static Menu TargetSelectorMenu()
        {
            return Menu.AddSubMenu(new Menu("Target Selector", "TargetSelector"));
        }

        #endregion
    }
    public class Standards
    {
        #region Static Fields

        protected static readonly Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                                         {
                                                                             {
                                                                                 Spells.Q,
                                                                                 new Spell(
                                                                                 SpellSlot.Q,
                                                                                 Orbwalking.GetRealAutoAttackRange(
                                                                                     Player) + 100)
                                                                             },
                                                                             {
                                                                                 Spells.W,
                                                                                 new Spell(
                                                                                 SpellSlot.W,
                                                                                 400 + Player.BoundingRadius)
                                                                             },
                                                                             {
                                                                                 Spells.E,
                                                                                 new Spell(
                                                                                 SpellSlot.E,
                                                                                 1000 + Player.BoundingRadius)
                                                                             },
                                                                             { Spells.R, new Spell(SpellSlot.R, 2000) }
                                                                         };

        public static int LastSwitch;

        public static Orbwalking.Orbwalker Orbwalker;

        protected static SpellSlot Ignite;

        #endregion

        #region Public Properties

        public static int Ferocity => (int)ObjectManager.Player.Mana;

        public static bool HasPassive => Player.Buffs.Any(x => x.Name.ToLower().Contains("rengarpassivebuff"));

        public static AIHeroClient Player => ObjectManager.Player;

        public static bool RengarR => Player.Buffs.Any(x => x.Name.ToLower().Contains("rengarrbuff"));

        public static string ScriptVersion => typeof(Rengar).Assembly.GetName().Version.ToString();

        #endregion

        #region Public Methods and Operators

        public static float IgniteDamage(AIHeroClient target)
        {
            return Ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready
                       ? 0f
                       : (float)Player.GetSummonerSpellDamage(target, Damage.DamageSummonerSpell.Ignite);
        }

        public static bool IsActive(string menuItem) => MenuInit.Menu.Item(menuItem).IsActive();

        #endregion

        #region Methods

        public static StringList IsListActive(string menuItem) => MenuInit.Menu.Item(menuItem).GetValue<StringList>();

        #endregion
    }
}
