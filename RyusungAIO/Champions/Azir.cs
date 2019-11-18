using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;

namespace DaoHungAIO.Champions
{
    using EnsoulSharp.Common;
    class AzirWalker : Orbwalking.Orbwalker
    {
        private static readonly AIHeroClient MyHero = ObjectManager.Player;
        public static readonly List<AIMinionClient> Soilders = new List<AIMinionClient>();

        public AzirWalker(Menu attachToMenu) : base(attachToMenu)
        {
        }

        public static double GetAzirAaSandwarriorDamage(AttackableUnit target)
        {
            var unit = (AIBaseClient)target;
            var dmg = MyHero.GetSpellDamage(unit, SpellSlot.W);

            var count = Soilders.Count(obj => obj.Position.Distance(unit.Position) < 350);

            if (count > 1)
                return dmg + dmg * (count - 1);

            return dmg;
        }

        public static bool InSoldierAttackRange(AttackableUnit target)
        {
            return Soilders.Count(obj => obj.Position.Distance(target.Position) < 350 && MyHero.Distance(target) < 1000 && !obj.IsMoving) > 0;
        }

        private static float GetAutoAttackRange(AIBaseClient source = null, AttackableUnit target = null)
        {
            if (source == null)
                source = MyHero;
            var ret = source.AttackRange + MyHero.BoundingRadius;
            if (target != null)
                ret += target.BoundingRadius;
            return ret;
        }

        public override bool InAutoAttackRange(AttackableUnit target)
        {
            if (!target.IsValidTarget())
                return false;
            if (Orbwalking.InAutoAttackRange(target))
                return true;
            if (!(target is AIBaseClient))
                return false;
            if (InSoldierAttackRange(target))
            {
                return true;
            }
            return false;
        }

        public override AttackableUnit GetTarget()
        {
            AttackableUnit tempTarget = null;

            if ((ActiveMode == Orbwalking.OrbwalkingMode.Mixed || ActiveMode == Orbwalking.OrbwalkingMode.Combo))
            {
                tempTarget = GetBestHeroTarget();
                if (tempTarget != null)
                    return tempTarget;
            }

            //last hit
            if (ActiveMode == Orbwalking.OrbwalkingMode.Mixed || ActiveMode == Orbwalking.OrbwalkingMode.LastHit || ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                foreach (var minion in from minion in ObjectManager.Get<AIMinionClient>().Where(minion => minion.IsValidTarget() && minion.Name != "Beacon" && InAutoAttackRange(minion)
                && minion.Health < 3 * (MyHero.BaseAttackDamage + MyHero.FlatPhysicalDamageMod))
                                       let t = (int)(MyHero.AttackCastDelay * 1000) - 100 + Game.Ping / 2
                                       let predHealth = HealthPrediction.GetHealthPrediction(minion, t, 0)
                                       where minion.Team != GameObjectTeam.Neutral && predHealth > 0 && predHealth <= (InSoldierAttackRange(minion) ? GetAzirAaSandwarriorDamage(minion) - 30 : MyHero.GetAutoAttackDamage(minion, true))
                                       select minion)
                    return minion;
            }

            //turret
            if (ActiveMode == Orbwalking.OrbwalkingMode.Mixed || ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {

                foreach (
                    var turret in
                        ObjectManager.Get<AITurretClient>().Where(turret => turret.IsValidTarget(GetAutoAttackRange(MyHero, turret))))
                    return turret;
            }

            //jungle
            if (ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                float[] maxhealth;
                if (MyHero.CharacterName == "Azir" && Soilders.Count > 0)
                {
                    maxhealth = new float[] { 0 };
                    var maxhealth1 = maxhealth;
                    var minions = MinionManager.GetMinions(ObjectManager.Player.Position, 800, MinionTypes.All, MinionTeam.Neutral);
                    foreach (
                        var minion in
                            minions
                                .Where(minion => InSoldierAttackRange(minion) && minion.Name != "Beacon" && minion.IsValidTarget())
                                .Where(minion => minion.MaxHealth >= maxhealth1[0] || Math.Abs(maxhealth1[0] - float.MaxValue) < float.Epsilon))
                    {
                        tempTarget = minion;
                        maxhealth[0] = minion.MaxHealth;
                    }
                    if (tempTarget != null)
                        return tempTarget;
                }

                maxhealth = new float[] { 0 };
                var maxhealth2 = maxhealth;
                foreach (var minion in ObjectManager.Get<AIMinionClient>().Where(minion => minion.IsValidTarget(GetAutoAttackRange(MyHero, minion)) && minion.Name != "Beacon" && minion.Team == GameObjectTeam.Neutral).Where(minion => minion.MaxHealth >= maxhealth2[0] || Math.Abs(maxhealth2[0] - float.MaxValue) < float.Epsilon))
                {
                    tempTarget = minion;
                    maxhealth[0] = minion.MaxHealth;
                }
                if (tempTarget != null)
                    return tempTarget;
            }

            if (ShouldWaits())
                return null;

            //lane clear
            if (ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                return (ObjectManager.Get<AIMinionClient>().Where(minion => minion.IsValidTarget() && InAutoAttackRange(minion))).MaxOrDefault(x => x.Health);
            }

            return null;
        }

        private bool ShouldWaits()
        {
            return ObjectManager.Get<AIMinionClient>()
            .Any(
            minion =>
            minion.IsValidTarget(850) && minion.Team != GameObjectTeam.Neutral &&
            InAutoAttackRange(minion) &&
            HealthPrediction.LaneClearHealthPrediction(minion, (int)((MyHero.AttackDelay * 1000) * 2f), 0) <=
            (InSoldierAttackRange(minion) ? GetAzirAaSandwarriorDamage(minion) - 30 : MyHero.GetAutoAttackDamage(minion, true)));
        }

        private AIHeroClient GetBestHeroTarget()
        {
            var bestTarget = HeroManager.Enemies.Where(InAutoAttackRange).OrderByDescending(GetAzirAaSandwarriorDamage).FirstOrDefault();

            return bestTarget ?? TargetSelector.GetTarget(GetAutoAttackRange(), TargetSelector.DamageType.Magical);
        }

        public static void OnDelete(GameObject sender, EventArgs args)
        {
            Soilders.RemoveAll(s => s.NetworkId == sender.NetworkId);
        }

        public static void Obj_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is AIMinionClient))
                return;

            if (sender.Name == "AzirSoldier" && sender.IsAlly)
            {
                AIMinionClient soldier = (AIMinionClient)sender;
                if (soldier.CharacterName == "AzirSoldier")
                    Soilders.Add(soldier);
            }
        }
    }
    internal static class Jumper
    {
        private static int CastQT = 0;
        private static Vector2 CastQLocation = new Vector2();

        private static int CastET = 0;
        private static Vector2 CastELocation = new Vector2();
        private static AIHeroClient player = ObjectManager.Player;

        static Jumper()
        {
            AIBaseClient.OnProcessSpellCast += AIBaseClient_OnProcessSpellCast;
        }

        static void AIBaseClient_OnProcessSpellCast(
    AIBaseClient sender,
    AIBaseClientProcessSpellCastEventArgs args
)
        {
            if (!sender.IsMe)
                return;

            
            if (args.SData.Name == "AzirE" && (Azir.Q.IsReady() || player.Spellbook.GetSpell(SpellSlot.Q).State == SpellState.NotAvailable))
            {
                if (Utils.TickCount - Azir.E.LastCastAttemptT < 0)
                    Azir.Q2.Cast(Game.CursorPosRaw);
            }
        }

        public static void Jump()
        {
            if (Math.Abs(Azir.E.Cooldown) < 0.00001)
            {
                Vector3 wVec = player.Position + Vector3.Normalize(Game.CursorPosRaw - player.Position) * Azir.W.Range;

                if ((Azir.E.IsReady() || player.Spellbook.GetSpell(SpellSlot.E).State == SpellState.NotAvailable))
                {
                    if (SoldiersManager.AllSoldiers2.Count < 1 && Azir.W.IsReady())
                        Azir.W.Cast(wVec);
                    else if (SoldiersManager.AllSoldiers2.Count < 1 && !Azir.W.IsReady())
                        return;

                    if (GetNearestSoilderToMouse() == null)
                        return;

                    var nearSlave = GetNearestSoilderToMouse();

                    if ((Azir.E.IsReady() || player.Spellbook.GetSpell(SpellSlot.E).State == SpellState.NotAvailable) &&
                        player.Distance(Game.CursorPosRaw) > Game.CursorPosRaw.Distance(nearSlave.Position))
                    {
                        Azir.E.Cast(nearSlave.Position);
                        Azir.E.LastCastAttemptT = Utils.TickCount + 250;
                    }
                    else if (Azir.W.IsReady())
                    {
                        Azir.W.Cast(wVec);
                    }
                }
            }
        }
        private static GameObject GetNearestSoilderToMouse()
        {
            var soilder = SoldiersManager.AllSoldiers2.OrderBy(x => Game.CursorPosRaw.Distance(x.Position));

            if (soilder.FirstOrDefault() != null)
                return soilder.FirstOrDefault();

            return null;
        }
    }
    internal static class SoldiersManager
    {
        private static List<AIMinionClient> _soldiers = new List<AIMinionClient>();
        private static Dictionary<int, string> Animations = new Dictionary<int, string>();
        private const bool DrawSoldiers = true;

        public static List<AIMinionClient> ActiveSoldiers
        {
            get { return _soldiers.Where(s => s.IsValid && !s.IsDead && !s.IsMoving && (!Animations.ContainsKey((int)s.NetworkId) || Animations[(int)s.NetworkId] != "Inactive")).ToList(); }
        }

        public static List<AIMinionClient> AllSoldiers2
        {
            get { return _soldiers.Where(s => s.IsValid && !s.IsDead).ToList(); }
        }

        public static List<AIMinionClient> AllSoldiers
        {
            get { return _soldiers.Where(s => s.IsValid && !s.IsDead && !s.IsMoving).ToList(); }
        }

        static SoldiersManager()
        {
            AIMinionClient.OnCreate += AIMinionClient_OnCreate;
            AIMinionClient.OnDelete += AIMinionClient_OnDelete;
            AIMinionClient.OnPlayAnimation += AIMinionClient_OnPlayAnimation;

            if (DrawSoldiers)
            {
                Drawing.OnDraw += Drawing_OnDraw;
            }
        }

        static void AIMinionClient_OnPlayAnimation(AIBaseClient sender,
    AIBaseClientPlayAnimationEventArgs args)
        {
            if (sender is AIMinionClient && ((AIMinionClient)sender).IsSoldier())
            {
                Animations[(int)sender.NetworkId] = args.Animation;
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var soldier in ActiveSoldiers)
            {
                Render.Circle.DrawCircle(soldier.Position, 320, System.Drawing.Color.FromArgb(150, System.Drawing.Color.Yellow));
            }
        }

        private static bool IsSoldier(this AIMinionClient soldier)
        {
            return soldier.IsAlly && String.Equals(soldier.CharacterName, "azirsoldier", StringComparison.InvariantCultureIgnoreCase);
        }

        static void AIMinionClient_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is AIMinionClient && ((AIMinionClient)sender).IsSoldier())
            {
                _soldiers.Add((AIMinionClient)sender);
            }
        }

        static void AIMinionClient_OnDelete(GameObject sender, EventArgs args)
        {
            _soldiers.RemoveAll(s => s.NetworkId == sender.NetworkId);
            Animations.Remove((int)sender.NetworkId);
        }
    }
    class Azir
    {
        public static AIHeroClient Player;
        public static Menu Menu;
        public static AzirWalker AzirWalker;

        public static Spell Q;
        public static Spell Q2;
        public static Spell Qline;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static SpellSlot IgniteSlot;

        private static int _allinT = 0;


        public Azir()
        {
            Player = ObjectManager.Player;

            #region Spells
            Q = new Spell(SpellSlot.Q, 825);
            Q2 = new Spell(SpellSlot.Q, 2000);
            Qline = new Spell(SpellSlot.Q, 825);

            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 1250);
            R = new Spell(SpellSlot.R, 450);

            Q.SetSkillshot(0, 70, 1600, false, SkillshotType.SkillshotCircle);
            Q2.SetSkillshot(0, 80, 1600, false, SkillshotType.SkillshotCircle);
            Qline.SetSkillshot(0, 70, 1600, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0, 100, 1700, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.5f, 0, 1400, false, SkillshotType.SkillshotLine);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            #endregion

            #region Menu
            Menu = new Menu("Azir", "Azir", true);

            TargetSelector.AddToMenu(Menu.SubMenu("Target Selector"));
            AzirWalker = new AzirWalker(Menu.SubMenu("Orbwalker"));

            Menu.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseEC", "Use E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseRC", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("AllInKEK", "All-in (tap)!").SetValue(new KeyBind('G', KeyBindType.Press)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassMinMana", "Min mana %").SetValue(new Slider(20, 0, 100)));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind('C', KeyBindType.Press)));

            Menu.SubMenu("LaneClear").AddItem(new MenuItem("UseQLC", "Use Q").SetValue(true));
            Menu.SubMenu("LaneClear").AddItem(new MenuItem("UseWLC", "Use W").SetValue(true));
            Menu.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind('V', KeyBindType.Press)));

            Menu.SubMenu("Misc").AddItem(new MenuItem("Jump", "Jump towards cursor").SetValue(new KeyBind('E', KeyBindType.Press)));
            Menu.SubMenu("Misc").Item("Jump").ValueChanged += Azir_ValueChanged;
            Menu.SubMenu("Misc").AddItem(new MenuItem("AutoEInterrupt", "Interrupt targets with E").SetValue(false));

            Menu.SubMenu("R").AddItem(new MenuItem("AutoRN", "Auto R if it will hit >=").SetValue(new Slider(3, 1, 6)));
            Menu.SubMenu("R").AddItem(new MenuItem("AutoRInterrupt", "Interrupt targets with R").SetValue(true));

            var dmgAfterComboItem = new MenuItem("DamageAfterR", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit += hero => GetComboDamage(hero);
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Menu.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(150, Color.Yellow))));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(150, Color.Yellow))));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Yellow))));
            Menu.SubMenu("Drawings").AddItem(dmgAfterComboItem);

            #endregion
            Interrupters.OnInterrupter += Interrupter2_OnInterruptableTarget;
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Azir_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            if (e.GetNewValue<KeyBind>().Active)
            {
                Jumper.Jump();
            }
        }

        static void Interrupter2_OnInterruptableTarget(ActiveInterrupter interrupter)
        {
            AIHeroClient sender = interrupter.Sender;
            if (interrupter.DangerLevel != InterrupterDangerLevel.High)
            {
                return;
            }

            if (Menu.SubMenu("Misc").Item("AutoEInterrupt").GetValue<bool>() && E.IsReady())
            {
                foreach (var soldier in SoldiersManager.AllSoldiers.Where(s => Player.Distance(s, true) < E.RangeSqr))
                {
                    if (E.WillHit(sender, soldier.Position))
                    {
                        E.Cast(soldier.Position);
                        return;
                    }
                }
                return;
            }

            if (Menu.SubMenu("R").Item("AutoRInterrupt").GetValue<bool>() && R.IsReady())
            {
                var dist = Player.Distance(sender, true);

                if (dist < R.RangeSqr)
                {
                    R.Cast(sender, false, true);
                    return;
                }

                if (dist < Math.Pow(Math.Sqrt(R.RangeSqr + Math.Pow(R.Width + sender.BoundingRadius, 2)), 2))
                {
                    var angle = (float)Math.Atan(R.Width + sender.BoundingRadius / R.Range);
                    var p = (sender.Position.To2D() - Player.Position.To2D()).Rotated(angle);
                    R.Cast(p);
                }
            }
        }

        static float GetComboDamage(AIBaseClient target)
        {
            var damage = 0d;
            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            }

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.GetSpell(IgniteSlot).State == SpellState.Ready)
            {
                damage += Player.GetSummonerSpellDamage(target, Damage.DamageSummonerSpell.Ignite);
            }

            damage += SoldiersManager.ActiveSoldiers.Count * Player.GetSpellDamage(target, SpellSlot.W);

            return (float)damage;
        }

        static void LaneClear()
        {
            var useQ = Menu.SubMenu("LaneClear").Item("UseQLC").GetValue<bool>();
            var useW = Menu.SubMenu("LaneClear").Item("UseWLC").GetValue<bool>();

            var minions = MinionManager.GetMinions(Q.Range);
            if (minions.Count == 0)
            {
                minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            }

            if (minions.Count > 0)
            {
                if (useW && W.Instance.Ammo > 0 && (minions.Count > 2 || minions[0].Team == GameObjectTeam.Neutral))
                {
                    var p = Player.Position.To2D().Extend(minions[0].Position.To2D(), W.Range);
                    W.Cast(p);
                    return;
                }

                if (useQ && Qline.IsReady() && (minions.Count >= 2 || minions[0].Team == GameObjectTeam.Neutral))
                {
                    var positions = new Dictionary<Vector3, int>();

                    foreach (var soldier in SoldiersManager.AllSoldiers)
                    {
                        Qline.UpdateSourcePosition(soldier.Position, ObjectManager.Player.Position);
                        foreach (var minion in minions)
                        {
                            var hits = Qline.CountHits(minions.Select(m => m.Position).ToList(), minion.Position);
                            if (hits >= 2 || minions[0].Team == GameObjectTeam.Neutral)
                            {
                                if (!positions.ContainsKey(minion.Position))
                                {
                                    positions.Add(minion.Position, hits);
                                }
                            }
                        }
                    }

                    if (positions.Count > 0)
                    {
                        Qline.Cast(positions.MaxOrDefault(k => k.Value).Key);
                    }
                }
                return;
            }
        }

        static void Harass()
        {
            var harassTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (harassTarget == null)
            {
                return;
            }

            if (W.Instance.Ammo > 0)
            {
                var p = Player.Position.To2D().Extend(harassTarget.Position.To2D(), W.Range);
                if (Q.IsReady() || HeroManager.Enemies.Any(h => h.IsValidTarget(W.Range + 200)))
                {
                    W.Cast(p);
                }
                return;
            }

            if (Q.IsReady())
            {
                var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (qTarget != null)
                {
                    foreach (var soldier in SoldiersManager.AllSoldiers)
                    {
                        Q.UpdateSourcePosition(soldier.Position, ObjectManager.Player.Position);
                        Q.Cast(qTarget);
                    }
                }
            }
        }

        static void Combo()
        {
            var useQ = Menu.SubMenu("Combo").Item("UseQC").GetValue<bool>();
            var useW = Menu.SubMenu("Combo").Item("UseWC").GetValue<bool>();
            var useE = Menu.SubMenu("Combo").Item("UseEC").GetValue<bool>();
            var useR = (Utils.TickCount - _allinT < 4000) && Menu.SubMenu("Combo").Item("UseRC").GetValue<bool>();

            var qTarget = TargetSelector.GetTarget(Q.Range + 200, TargetSelector.DamageType.Magical);
            if (qTarget == null)
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                foreach (var soldier in SoldiersManager.AllSoldiers)
                {
                    Q.UpdateSourcePosition(soldier.Position, ObjectManager.Player.Position);
                    Q.Cast(qTarget);
                }
            }

            if (useW && W.Instance.Ammo > 0)
            {
                var p = Player.Distance(qTarget, true) > W.RangeSqr ? Player.Position.To2D().Extend(qTarget.Position.To2D(), W.Range) : qTarget.Position.To2D();
                W.Cast(p);
            }

            if (useE && ((Utils.TickCount - _allinT) < 4000 || (HeroManager.Enemies.Count(e => e.IsValidTarget(1000)) <= 2 && GetComboDamage(qTarget) > qTarget.Health)) && E.IsReady())
            {
                foreach (var soldier in SoldiersManager.AllSoldiers2.Where(s => Player.Distance(s, true) < E.RangeSqr))
                {
                    if (E.WillHit(qTarget, soldier.Position))
                    {
                        E.Cast(soldier.Position);
                        return;
                    }
                }
            }

            if (GetComboDamage(qTarget) > qTarget.Health)
            {
                if (useR && R.IsReady())
                {
                    R.Cast(qTarget, false, true);
                }

                if (Menu.SubMenu("Combo").Item("UseIgnite").GetValue<bool>() && IgniteSlot != SpellSlot.Unknown && Player.GetSpell(IgniteSlot).State == SpellState.Ready && Player.Distance(qTarget, true) < 600 * 600)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, qTarget);
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            R.Width = 133 * (3 + R.Level);

            var minTargets = Menu.SubMenu("R").Item("AutoRN").GetValue<Slider>().Value;
            if (minTargets != 6)
            {
                R.CastIfWillHit(R.GetTarget(), minTargets);
            }

            if (Menu.SubMenu("Combo").Item("AllInKEK").GetValue<KeyBind>().Active)
            {
                _allinT = Utils.TickCount;
            }

            if (Menu.SubMenu("Harass").Item("HarassActive").GetValue<KeyBind>().Active && Player.ManaPercent > Menu.SubMenu("Harass").Item("HarassMinMana").GetValue<Slider>().Value)
            {
                Harass();
                return;
            }

            if (Menu.SubMenu("Combo").Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
                return;
            }

            if (Menu.SubMenu("LaneClear").Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                LaneClear();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var qCircle = Menu.SubMenu("Drawings").Item("QRange").GetValue<Circle>();
            if (qCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, qCircle.Color);
            }

            var wCircle = Menu.SubMenu("Drawings").Item("WRange").GetValue<Circle>();
            if (wCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, wCircle.Color);
            }

            var rCircle = Menu.SubMenu("Drawings").Item("RRange").GetValue<Circle>();
            if (rCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, rCircle.Color);
            }
        }
    }
}
