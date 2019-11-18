using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;

namespace DaoHungAIO.Champions
{
    class Diana
    {
        public static Orbwalking.Orbwalker Orbwalker;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                     { Spells.Q, new Spell(SpellSlot.Q, 830) },
                                                                     { Spells.W, new Spell(SpellSlot.W, 250) },
                                                                     { Spells.E, new Spell(SpellSlot.E, 450) },
                                                                     { Spells.R, new Spell(SpellSlot.R, 825) }
                                                             };

        private static SpellSlot ignite;



        #region Public Properties

        public static string ScriptVersion => typeof(Diana).Assembly.GetName().Version.ToString();

        #endregion

        #region Properties

        private static HitChance CustomHitChance => GetHitchance();

        private static AIHeroClient Player => ObjectManager.Player;

        #endregion

        #region Public Methods and Operators

        public static float GetComboDamage(AIBaseClient enemy)
        {
            float damage = 0;

            if (spells[Spells.Q].IsReady())
            {
                damage += spells[Spells.Q].GetDamage(enemy);
            }

            if (spells[Spells.W].IsReady())
            {
                damage += spells[Spells.W].GetDamage(enemy);
            }

            if (spells[Spells.E].IsReady())
            {
                damage += spells[Spells.E].GetDamage(enemy);
            }

            if (spells[Spells.R].IsReady())
            {
                damage += spells[Spells.R].GetDamage(enemy);
            }

            if (ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) != SpellState.Ready)
            {
                damage += (float)Player.GetSummonerSpellDamage(enemy, Damage.DamageSummonerSpell.Ignite);
            }

            return damage;
        }

        public Diana()
        {


            spells[Spells.Q].SetSkillshot(0.25f, 185f, 1620f, false, SkillshotType.SkillshotCircle);
            ignite = Player.GetSpellSlot("summonerdot");

            ElDianaMenu.Initialize();
            Game.OnTick += OnUpdate;
            Drawing.OnDraw += Drawings.Drawing_OnDraw;

            Gapclosers.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupters.OnInterrupter += OnInterrupterHandle;
            

            CustomEvents.Unit.OnDash += (source, eventArgs) =>
            {
                if (!source.IsEnemy)
                {
                    return;
                }

                var eSlot = spells[Spells.E];
                var dis = Player.Distance(source);
                if (!eventArgs.Unit.IsDashing() && ElDianaMenu.Menu.Item("ElDiana.Interrupt.UseEDashes").GetValue<bool>()
                    && eSlot.IsReady() && eSlot.Range >= dis)
                {
                    eSlot.Cast();
                }
            };
        }

        private void OnInterrupterHandle(ActiveInterrupter interrupter)
        {
            if (interrupter.Sender.IsEnemy) {
                var eSlot = spells[Spells.E];
                if (ElDianaMenu.Menu.Item("ElDiana.Interrupt.UseEInterrupt").GetValue<bool>() && eSlot.IsReady()
                    && eSlot.Range >= Player.Distance(interrupter.Sender))
                {
                    eSlot.Cast();
                }
            }
            
  
        }

        #endregion

        #region Methods

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsEnemy) {
                if (!gapcloser.Sender.IsValidTarget(spells[Spells.E].Range))
                {
                    return;
                }

                if (gapcloser.Sender.IsValidTarget(spells[Spells.E].Range))
                {
                    if (IsActive("ElDiana.Interrupt.G") && spells[Spells.E].IsReady())
                    {
                        spells[Spells.E].Cast(gapcloser.Sender);
                    }
                }
            }

        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (IsActive("ElDiana.Combo.Q") && spells[Spells.Q].IsReady())
            {
                if (Player.Distance(target) <= spells[Spells.Q].Range)
                {
                    var prediction = spells[Spells.Q].GetPrediction(target);

                    if (prediction.Hitchance >= CustomHitChance)
                    {
                        spells[Spells.Q].Cast(prediction.CastPosition);
                    }
                }
            }

            if (IsActive("ElDiana.Combo.QR"))
            {
                var killableTarget =
                    HeroManager.Enemies.FirstOrDefault(
                        x => spells[Spells.R].IsKillable(x) && x.Distance(Player) <= spells[Spells.R].Range * 2);

                if (killableTarget != null)
                {
                    GapCloser(killableTarget);
                }
            }

            if (IsActive("ElDiana.Combo.R") && spells[Spells.R].IsReady())
            {
                if (Player.Distance(target) <= spells[Spells.R].Range)
                {
                    if (HasQBuff(target)
                        && (!target.UnderTurret(true)
                            || (ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value
                                <= Player.HealthPercent)))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
            }

            if (IsActive("ElDiana.Combo.W") && spells[Spells.W].IsReady())
            {
                if (Player.IsDashing() || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    return;
                }

                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.Combo.E") && spells[Spells.E].IsReady())
            {
                if (Player.IsDashing() || Player.Distance(target) > spells[Spells.E].Range)
                {
                    return;
                }

                spells[Spells.E].Cast();
            }

            if (IsActive("ElDiana.Combo.Secure")
                && (!target.UnderTurret(true)
                    || (ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value
                        <= Player.HealthPercent)))
            {
                var closeEnemies = Player.GetEnemiesInRange(spells[Spells.R].Range * 2).Count;

                if (closeEnemies <= ElDianaMenu.Menu.Item("ElDiana.Combo.UseSecondRLimitation").GetValue<Slider>().Value
                    && IsActive("ElDiana.Combo.R") && !spells[Spells.Q].IsReady() && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target)
                        && (!target.UnderTurret(true)
                            || (ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value
                                <= Player.HealthPercent)))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }

                if (closeEnemies <= ElDianaMenu.Menu.Item("ElDiana.Combo.UseSecondRLimitation").GetValue<Slider>().Value
                    && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
            }

            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health
                && IsActive("ElDiana.Combo.Ignite"))
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }

        private static void GapCloser(AIBaseClient target)
        {
            if (target == null || !spells[Spells.R].IsInRange(target))
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && spells[Spells.R].IsReady())
            {
                var closeMinion =
                    MinionManager.GetMinions(Player.Position, spells[Spells.R].Range)
                        .OrderBy(x => x.Distance(target))
                        .FirstOrDefault(x => !spells[Spells.Q].IsKillable(x));

                if (closeMinion != null)
                {
                    spells[Spells.Q].Cast(closeMinion);
                    if (HasQBuff(closeMinion))
                    {
                        spells[Spells.R].Cast(closeMinion);
                    }
                }
            }
        }

        private static HitChance GetHitchance()
        {
            switch (ElDianaMenu.Menu.Item("ElDiana.hitChance").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.VeryHigh;
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }

            if (Player.ManaPercent <= ElDianaMenu.Menu.Item("ElDiana.Harass.Mana").GetValue<Slider>().Value)
            {
                return;
            }

            if (IsActive("ElDiana.Harass.Q") && spells[Spells.Q].IsReady() && spells[Spells.Q].IsInRange(target))
            {
                var pred = spells[Spells.Q].GetPrediction(target);
                if (pred.Hitchance >= CustomHitChance)
                {
                    spells[Spells.Q].Cast(target);
                }
            }

            if (IsActive("ElDiana.Harass.W") && spells[Spells.W].IsReady() && spells[Spells.W].IsInRange(target))
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.Harass.E") && spells[Spells.E].IsReady()
                && Player.Distance(target) <= spells[Spells.E].Range)
            {
                spells[Spells.E].Cast();
            }
        }

        private static bool HasQBuff(AIBaseClient target)
        {
            return target.HasBuff("dianamoonlight");
        }

        private static float IgniteDamage(AIHeroClient target)
        {
            if (ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.DamageSummonerSpell.Ignite);
        }

        private static bool IsActive(string menuItem)
        {
            return ElDianaMenu.Menu.Item(menuItem).IsActive();
        }

        private static void JungleClear()
        {
            var minions = MinionManager.GetMinions(
                ObjectManager.Player.Position,
                spells[Spells.Q].Range,
                MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            var qMinions = minions.FindAll(minion => minion.IsValidTarget(spells[Spells.Q].Range));
            var qMinion = qMinions.FirstOrDefault();

            if (qMinion == null)
            {
                return;
            }

            if (IsActive("ElDiana.JungleClear.Q") && spells[Spells.Q].IsReady())
            {
                if (qMinion.IsValidTarget())
                {
                    spells[Spells.Q].Cast(qMinion);
                }
            }

            if (IsActive("ElDiana.JungleClear.W") && spells[Spells.W].IsReady())
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.JungleClear.E") && spells[Spells.E].IsReady()
                && qMinions.Count(m => Player.Distance(m) < spells[Spells.W].Range) < 1)
            {
                spells[Spells.E].Cast();
            }

            if (IsActive("ElDiana.JungleClear.R") && spells[Spells.R].IsReady())
            {
                var moonlightMob =
                    minions.FindAll(minion => HasQBuff(minion)).OrderBy(minion => minion.HealthPercent);
                if (moonlightMob.Any())
                {
                    var canBeKilled = moonlightMob.Find(minion => minion.Health < spells[Spells.R].GetDamage(minion));
                    if (canBeKilled.IsValidTarget())
                    {
                        spells[Spells.R].Cast(canBeKilled);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var minion =
                MinionManager.GetMinions(ObjectManager.Player.Position, spells[Spells.Q].Range).FirstOrDefault();
            if (minion == null || minion.Name.ToLower().Contains("ward"))
            {
                return;
            }

            var countQ = ElDianaMenu.Menu.Item("ElDiana.LaneClear.Count.Minions.Q").GetValue<Slider>().Value;
            var countW = ElDianaMenu.Menu.Item("ElDiana.LaneClear.Count.Minions.W").GetValue<Slider>().Value;
            var countE = ElDianaMenu.Menu.Item("ElDiana.LaneClear.Count.Minions.E").GetValue<Slider>().Value;

            var minions = MinionManager.GetMinions(
                ObjectManager.Player.Position,
                spells[Spells.Q].Range,
                MinionTypes.All,
                MinionTeam.NotAlly);

            var qMinions = minions.FindAll(minionQ => minion.IsValidTarget(spells[Spells.Q].Range));
            var qMinion = qMinions.Find(minionQ => minionQ.IsValidTarget());

            if (IsActive("ElDiana.LaneClear.Q") && spells[Spells.Q].IsReady()
                && spells[Spells.Q].GetCircularFarmLocation(minions).MinionsHit >= countQ)
            {
                spells[Spells.Q].Cast(qMinion);
            }

            if (IsActive("ElDiana.LaneClear.W") && spells[Spells.W].IsReady()
                && spells[Spells.W].GetCircularFarmLocation(minions).MinionsHit >= countW)
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.LaneClear.E") && spells[Spells.E].IsReady() && Player.Distance(qMinion, false) < 200
                && spells[Spells.E].GetCircularFarmLocation(minions).MinionsHit >= countE)
            {
                spells[Spells.E].Cast();
            }

            var minionsR = MinionManager.GetMinions(
                ObjectManager.Player.Position,
                spells[Spells.Q].Range,
                MinionTypes.All,
                MinionTeam.NotAlly,
                MinionOrderTypes.MaxHealth);

            if (IsActive("ElDiana.LaneClear.R") && spells[Spells.R].IsReady())
            {
                var moonlightMob = minionsR.FindAll(x => HasQBuff(x)).OrderBy(x => minion.HealthPercent);
                if (moonlightMob.Any())
                {
                    var canBeKilled = moonlightMob.Find(x => minion.Health < spells[Spells.R].GetDamage(minion));
                    if (canBeKilled.IsValidTarget())
                    {
                        spells[Spells.R].Cast(canBeKilled);
                    }
                }
            }
        }

        private static void Lasthit()
        {
            var qKillableMinion =
                MinionManager.GetMinions(
                    ObjectManager.Player.Position,
                    spells[Spells.Q].Range,
                    MinionTypes.All,
                    MinionTeam.NotAlly,
                    MinionOrderTypes.MaxHealth).FirstOrDefault(x => spells[Spells.Q].IsKillable(x));

            if (qKillableMinion == null
                || Player.ManaPercent <= ElDianaMenu.Menu.Item("ElDiana.LastHit.Mana").GetValue<Slider>().Value)
            {
                return;
            }

            spells[Spells.Q].Cast(qKillableMinion);
        }

        private static void MisayaCombo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }

            var minHpToDive = ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value;

            var useR = ElDianaMenu.Menu.Item("ElDiana.Combo.R").GetValue<bool>()
                       && (!target.UnderTurret(true) || (minHpToDive <= Player.HealthPercent));
            var useIgnite = ElDianaMenu.Menu.Item("ElDiana.Combo.Ignite").GetValue<bool>();
            var secondR = ElDianaMenu.Menu.Item("ElDiana.Combo.Secure").GetValue<bool>()
                          && (!target.UnderTurret(true) || (minHpToDive <= Player.HealthPercent));
            var distToTarget = Player.Distance(target, false);
            var misayaMinRange = ElDianaMenu.Menu.Item("ElDiana.Combo.R.MisayaMinRange").GetValue<Slider>().Value;
            var useSecondRLimitation =
                ElDianaMenu.Menu.Item("ElDiana.Combo.UseSecondRLimitation").GetValue<Slider>().Value;

            if (useR && spells[Spells.R].IsReady() && distToTarget > spells[Spells.R].Range)
            {
                return;
            }

            if (IsActive("ElDiana.Combo.Q") && useR && spells[Spells.Q].IsReady() && spells[Spells.R].IsReady()
                && distToTarget >= misayaMinRange)
            {
                spells[Spells.R].Cast(target);
                spells[Spells.Q].Cast(target);
                return;
            }

            if (IsActive("ElDiana.Combo.Q") && spells[Spells.Q].IsReady()
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                spells[Spells.Q].CastIfHitchanceEquals(target, CustomHitChance);
            }

            if (useR && spells[Spells.R].IsReady() && target.IsValidTarget(spells[Spells.R].Range)
                && HasQBuff(target))
            {
                spells[Spells.R].Cast(target);
            }

            if (IsActive("ElDiana.Combo.W") && spells[Spells.W].IsReady()
                && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)))
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.Combo.E") && spells[Spells.E].IsReady() && target.IsValidTarget(400f))
            {
                spells[Spells.E].Cast();
            }

            if (secondR)
            {
                var closeEnemies = Player.GetEnemiesInRange(spells[Spells.R].Range * 2).Count;

                if (closeEnemies <= useSecondRLimitation && useR && !spells[Spells.Q].IsReady()
                    && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }

                if (closeEnemies <= useSecondRLimitation && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
            }

            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health && useIgnite)
            {
                Player.Spellbook.CastSpell(ignite, target);
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
                    var ultType = ElDianaMenu.Menu.Item("ElDiana.Combo.R.Mode").GetValue<StringList>().SelectedIndex;
                    if (ElDianaMenu.Menu.Item("ElDiana.Hotkey.ToggleComboMode").GetValue<KeyBind>().Active)
                    {
                        ultType = (ultType + 1) % 2;
                    }
                    switch (ultType)
                    {
                        case 0:
                            Combo();
                            break;

                        case 1:
                            MisayaCombo();
                            break;
                    }
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Lasthit();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }
        }

        #endregion
    }
    public class ElDianaMenu
    {
        #region Static Fields

        public static Menu Menu;

        #endregion

        #region Public Methods and Operators

        public static void Initialize()
        {
            Menu = new Menu("DH.Diana credit JQuery", "menu", true);

            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            Diana.Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            Menu.AddSubMenu(orbwalkerMenu);

            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);

            Menu.AddSubMenu(targetSelector);

            var cMenu = new Menu("Combo", "Combo");
            cMenu.SubMenu("R")
                .AddItem(
                    new MenuItem("ElDiana.Combo.R.Mode", "Mode").SetValue(
                        new StringList(new[] { "Normal (Q->R)", "Misaya Combo (R->Q)" })));
            cMenu.SubMenu("R").AddItem(new MenuItem("ElDiana.Combo.R", "Use R").SetValue(true));
            cMenu.SubMenu("R")
                .AddItem(
                    new MenuItem("ElDiana.Combo.R.MisayaMinRange", "R Minimum Range for Misaya").SetValue(
                        new Slider(
                            Convert.ToInt32(Diana.spells[Spells.R].Range * 0.8),
                            0,
                            Convert.ToInt32(Diana.spells[Spells.R].Range))));
            cMenu.SubMenu("R")
                .AddItem(
                    new MenuItem("ElDiana.Combo.R.PreventUnderTower", "Don't use ult if HP% <").SetValue(
                        new Slider(20)));

            cMenu.AddItem(new MenuItem("ElDiana.Combo.Q", "Use Q").SetValue(true));
            cMenu.AddItem(new MenuItem("ElDiana.Combo.W", "Use W").SetValue(true));
            cMenu.AddItem(new MenuItem("ElDiana.Combo.E", "Use E").SetValue(true));
            cMenu.AddItem(new MenuItem("ElDiana.Combo.QR", "Use Q > R on minion to gapclose KS").SetValue(false));
            cMenu.AddItem(new MenuItem("ElDiana.Combo.Secure", "Use R to secure kill").SetValue(true));
            cMenu.AddItem(
                new MenuItem("ElDiana.Combo.UseSecondRLimitation", "Max close enemies for secure kill with R").SetValue(
                    new Slider(5, 1, 5)));
            cMenu.AddItem(new MenuItem("ElDiana.Combo.Ignite", "Use Ignite").SetValue(true));
            cMenu.AddItem(new MenuItem("ElDiana.ssssssssssss", ""));
            cMenu.AddItem(
                new MenuItem("ElDiana.hitChance", "Hitchance Q").SetValue(
                    new StringList(new[] { "Low", "Medium", "High", "Very High" }, 3)));

            var switchComboMenu =
                new MenuItem("ElDiana.Hotkey.ToggleComboMode", "Toggle Combo Mode Hotkey").SetValue(
                    new KeyBind(84, KeyBindType.Press));
            cMenu.AddItem(switchComboMenu);
            switchComboMenu.ValueChanged += (sender, eventArgs) =>
            {
                if (eventArgs.GetNewValue<KeyBind>().Active) Diana.Orbwalker.ActiveMode = Orbwalking.OrbwalkingMode.Combo;
                else Diana.Orbwalker.ActiveMode = Orbwalking.OrbwalkingMode.None;
            };

            Menu.AddSubMenu(cMenu);

            var hMenu = new Menu("Harass", "Harass");
            {
                hMenu.AddItem(new MenuItem("ElDiana.Harass.Q", "Use Q").SetValue(true));
                hMenu.AddItem(new MenuItem("ElDiana.Harass.W", "Use W").SetValue(true));
                hMenu.AddItem(new MenuItem("ElDiana.Harass.E", "Use E").SetValue(true));
                hMenu.AddItem(new MenuItem("ElDiana.Harass.Mana", "Minimum mana for harass")).SetValue(new Slider(50));
            }

            Menu.AddSubMenu(hMenu);

            var lMenu = new Menu("Laneclear", "Laneclear");
            {
                lMenu.AddItem(new MenuItem("ElDiana.LaneClear.Q", "Use Q").SetValue(true));
                lMenu.AddItem(new MenuItem("ElDiana.LaneClear.W", "Use W").SetValue(true));
                lMenu.AddItem(new MenuItem("ElDiana.LaneClear.E", "Use E").SetValue(true));
                lMenu.AddItem(new MenuItem("ElDiana.LaneClear.R", "Use R").SetValue(false));
                lMenu.AddItem(new MenuItem("xxx", ""));

                lMenu.AddItem(
                    new MenuItem("ElDiana.LaneClear.Count.Minions.Q", "Minions in range for Q").SetValue(
                        new Slider(2, 1, 5)));
                lMenu.AddItem(
                    new MenuItem("ElDiana.LaneClear.Count.Minions.W", "Minions in range for W").SetValue(
                        new Slider(2, 1, 5)));
                lMenu.AddItem(
                    new MenuItem("ElDiana.LaneClear.Count.Minions.E", "Minions in range for E").SetValue(
                        new Slider(2, 1, 5)));
            }

            Menu.AddSubMenu(lMenu);

            var lasthitMenu = new Menu("Lasthit", "lasthit");
            {
                lasthitMenu.AddItem(new MenuItem("ElDiana.LastHit.Q", "Use Q").SetValue(true));
                lasthitMenu.AddItem(new MenuItem("ElDiana.LastHit.Mana", "Minimum mana")).SetValue(new Slider(50));
            }

            Menu.AddSubMenu(lasthitMenu);

            var jMenu = new Menu("Jungleclear", "Jungleclear");
            {
                jMenu.AddItem(new MenuItem("ElDiana.JungleClear.Q", "Use Q").SetValue(true));
                jMenu.AddItem(new MenuItem("ElDiana.JungleClear.W", "Use W").SetValue(true));
                jMenu.AddItem(new MenuItem("ElDiana.JungleClear.E", "Use E").SetValue(true));
                jMenu.AddItem(new MenuItem("ElDiana.JungleClear.R", "Use R").SetValue(false));
            }

            Menu.AddSubMenu(jMenu);

            var interruptMenu = new Menu("Interrupt", "Interrupt");
            {
                interruptMenu.AddItem(new MenuItem("ElDiana.Interrupt.G", "Use E on gapcloser").SetValue(true));
                interruptMenu.AddItem(
                    new MenuItem("ElDiana.Interrupt.UseEInterrupt", "Use E to interrupt").SetValue(true));
                interruptMenu.AddItem(
                    new MenuItem("ElDiana.Interrupt.UseEDashes", "Use E to interrupt dashes").SetValue(true));
            }

            Menu.AddSubMenu(interruptMenu);

            var miscMenu = new Menu("Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("ElDiana.Draw.off", "Turn drawings off").SetValue(false));
                miscMenu.AddItem(new MenuItem("ElDiana.Draw.Q", "Draw Q").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElDiana.Draw.W", "Draw W").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElDiana.Draw.E", "Draw E").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElDiana.Draw.R", "Draw R").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElDiana.Draw.RMisaya", "Draw Misaya Combo Range").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElDiana.Draw.Text", "Draw Text").SetValue(true));
                miscMenu.AddItem(new MenuItem("ezeazeezaze", ""));

                var dmgAfterE = new MenuItem("ElDiana.DrawComboDamage", "Draw combo damage").SetValue(true);
                var drawFill =
                    new MenuItem("ElDiana.DrawColour", "Fill colour", true).SetValue(
                        new Circle(true, Color.FromArgb(204, 204, 0, 0)));
                miscMenu.AddItem(drawFill);
                miscMenu.AddItem(dmgAfterE);

                DrawDamage.DamageToUnit = Diana.GetComboDamage;
                DrawDamage.Enabled = dmgAfterE.GetValue<bool>();
                DrawDamage.Fill = drawFill.GetValue<Circle>().Active;
                DrawDamage.FillColor = drawFill.GetValue<Circle>().Color;

                dmgAfterE.ValueChanged +=
                    delegate (object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DrawDamage.Enabled = eventArgs.GetNewValue<bool>();
                    };

                drawFill.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
                {
                    DrawDamage.Fill = eventArgs.GetNewValue<Circle>().Active;
                    DrawDamage.FillColor = eventArgs.GetNewValue<Circle>().Color;
                };
            }
            Menu.AddSubMenu(miscMenu);

            Menu.AddItem(new MenuItem("422442fsaafs4242f", ""));
            Menu.AddItem(new MenuItem("422442fsaafsf", $"Version: {Diana.ScriptVersion}"));
            Menu.AddItem(new MenuItem("fsasfafsfsafsa", "Made By jQuery"));
            
            Console.WriteLine("Menu Loaded");
        }

        #endregion
    }
    internal class DrawDamage //by xSalice
    {
        #region Constants

        private const int Height = 8;

        private const int Width = 103;

        private const int XOffset = 10;

        private const int YOffset = 20;

        #endregion

        #region Static Fields

        public static Color Color = Color.Lime;

        public static bool Enabled = true;

        public static bool Fill = true;

        public static Color FillColor = Color.Goldenrod;

        private static readonly Render.Text Text = new Render.Text(0, 0, "", 14, SharpDX.Color.Red, "monospace");

        private static DamageToUnitDelegate _damageToUnit;

        #endregion

        #region Delegates

        public delegate float DamageToUnitDelegate(AIHeroClient hero);

        #endregion

        #region Public Properties

        public static DamageToUnitDelegate DamageToUnit
        {
            get
            {
                return _damageToUnit;
            }

            set
            {
                if (_damageToUnit == null)
                {
                    Drawing.OnDraw += Drawing_OnDraw;
                }
                _damageToUnit = value;
            }
        }

        #endregion

        #region Methods

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Enabled || _damageToUnit == null)
            {
                return;
            }

            foreach (var unit in HeroManager.Enemies.Where(h => h.IsValid && h.IsHPBarRendered))
            {
                var barPos = unit.HPBarPosition;
                var damage = _damageToUnit(unit);
                var percentHealthAfterDamage = Math.Max(0, unit.Health - damage) / unit.MaxHealth;
                var yPos = barPos.Y + YOffset;
                var xPosDamage = barPos.X + XOffset + Width * percentHealthAfterDamage;
                var xPosCurrentHp = barPos.X + XOffset + Width * unit.Health / unit.MaxHealth;

                if (damage > unit.Health)
                {
                    Text.X = (int)barPos.X + XOffset;
                    Text.Y = (int)barPos.Y + YOffset - 13;
                    Text.text = "Killable: " + (unit.Health - damage);
                    Text.OnEndScene();
                }

                Drawing.DrawLine(xPosDamage, yPos, xPosDamage, yPos + Height, 1, Color);

                if (Fill)
                {
                    var differenceInHP = xPosCurrentHp - xPosDamage;
                    var pos1 = barPos.X + 9 + (107 * percentHealthAfterDamage);

                    for (var i = 0; i < differenceInHP; i++)
                    {
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 1, FillColor);
                    }
                }
            }
        }

        #endregion
    }
    internal class Drawings
    {
        #region Public Methods and Operators

        public static void Drawing_OnDraw(EventArgs args)
        {
            var drawOff = ElDianaMenu.Menu.Item("ElDiana.Draw.off").GetValue<bool>();
            var drawQ = ElDianaMenu.Menu.Item("ElDiana.Draw.Q").GetValue<Circle>();
            var drawW = ElDianaMenu.Menu.Item("ElDiana.Draw.W").GetValue<Circle>();
            var drawE = ElDianaMenu.Menu.Item("ElDiana.Draw.E").GetValue<Circle>();
            var drawR = ElDianaMenu.Menu.Item("ElDiana.Draw.R").GetValue<Circle>();
            var drawRMisaya = ElDianaMenu.Menu.Item("ElDiana.Draw.RMisaya").GetValue<Circle>();
            var misayaRange = ElDianaMenu.Menu.Item("ElDiana.Combo.R.MisayaMinRange").GetValue<Slider>().Value;

            if (drawOff)
            {
                return;
            }

            if (drawQ.Active)
            {
                if (Diana.spells[Spells.Q].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.Q].Range, Color.White);
                }
            }

            if (drawE.Active)
            {
                if (Diana.spells[Spells.E].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.E].Range, Color.White);
                }
            }

            if (drawW.Active)
            {
                if (Diana.spells[Spells.W].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.W].Range, Color.White);
                }
            }

            if (drawR.Active)
            {
                if (Diana.spells[Spells.R].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.R].Range, Color.White);
                }
            }

            if (drawRMisaya.Active)
            {
                if (Diana.spells[Spells.R].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, misayaRange, Color.White);
                }
            }
        }

        #endregion
    }
}
