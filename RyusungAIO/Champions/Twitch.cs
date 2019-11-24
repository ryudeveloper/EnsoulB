using System;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using Utility = EnsoulSharp.SDK.Utility;
using SharpDX;
using SPrediction;
using Geometry = EnsoulSharp.SDK.Geometry;
using System.Linq;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;

namespace RyusungAIO.Champions
{
    public class Twitch
    {
        private static Menu Menu;
        private static AIHeroClient Player = ObjectManager.Player;
        private static Spell Q, Q1, W, E, R;
        private static bool AAPassive;
        private static bool HEXQ => Menu["Harass"].GetValue<MenuBool>("HEXQ");
        private static bool KillstealQ => Menu["killsteal"].GetValue<MenuBool>("KillstealQ");
        private static bool CQ => Menu["Combo"].GetValue<MenuBool>("CQ");
        private static bool CW => Menu["Combo"].GetValue<MenuBool>("CW");
        private static string CE => Menu["Combo"].GetValue<MenuList>("CE").SelectedValue;
        private static bool HQ => Menu["Harass"].GetValue<MenuBool>("HQ");
        private static bool HW => Menu["Harass"].GetValue<MenuBool>("HW");
        private static string HE => Menu["Harass"].GetValue<MenuList>("HE").SelectedValue;
        private static int HMinMana => Menu["Harass"].GetValue<MenuSlider>("HMinMana").Value;
        private static bool JQ => Menu["JungleClear"].GetValue<MenuBool>("JQ");
        private static bool JW => Menu["JungleClear"].GetValue<MenuBool>("JW");
        private static bool JE => Menu["JungleClear"].GetValue<MenuBool>("JE");
        private static bool LHQ => Menu["LaneClear"].GetValue<MenuBool>("LHQ");
        private static int LQ => Menu["LaneClear"].GetValue<MenuSlider>("LQ").Value;
        private static bool LW => Menu["LaneClear"].GetValue<MenuBool>("LW");
        private static bool LE => Menu["LaneClear"].GetValue<MenuBool>("LE");
        private static int LMinMana => Menu["LaneClear"].GetValue<MenuSlider>("LMinMana").Value;
        private static bool Dind => Menu["Draw"].GetValue<MenuBool>("Dind");
        private static bool DEQ => Menu["Draw"].GetValue<MenuBool>("DEQ");
        private static bool DQ => Menu["Draw"].GetValue<MenuBool>("DQ");
        private static bool DW => Menu["Draw"].GetValue<MenuBool>("DW");
        private static bool DE => Menu["Draw"].GetValue<MenuBool>("DE");
        static bool AutoQ => Menu["Auto"].GetValue<MenuKeyBind>("AutoQ").Active;
        private static int MinMana => Menu["Auto"].GetValue<MenuSlider>("MinMana").Value;
        private static int HHMinMana => Menu["Harass"].GetValue<MenuSlider>("HHMinMana").Value;
        private static int Humanizer => Menu["Misc"].GetValue<MenuSlider>("Humanizer").Value;
        static bool ForceR => Menu["Combo"].GetValue<MenuKeyBind>("ForceR").Active;
        static bool LT => Menu["LaneClear"].GetValue<MenuKeyBind>("LT").Active;
        static bool SP => Menu["Misc"].GetValue<MenuBool>("SkipPassive");

        public Twitch()
        {
            if (Player.CharacterName != "Twitch") return;
            Q = new Spell(SpellSlot.Q, 500);
            Q1 = new Spell(SpellSlot.Q, 1200);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, 300);

            OnMenuLoad();

            Q.SetTargetted(0.25f, 1400f);
            Q1.SetSkillshot(0.5f, 50, float.MaxValue, false, false, SkillshotType.Line);
            W.SetSkillshot(0.30f, 80f, 1600f, true, false, SkillshotType.Line);
            R.SetSkillshot(0.2f, 110f, 2500, true, false, SkillshotType.Line);

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnUpdate;
            //Drawing.OnEndScene += Drawing_OnEndScene;
            AIBaseClient.OnDoCast += OnDoCast;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnDoCast += OnDoCastLC;
            AIBaseClient.OnBuffGain += OnBuffGain;
            AIBaseClient.OnBuffLose += OnBuffGain;
            //Orbwalker.OnAction += OnActionDelegate;
            //AIBaseClient.OnBasicAttack += OnBasicAttack;
        }

        //private static void OnActionDelegate(Object sender, OrbwalkerActionArgs args)
        //{
        //    if (args.Type == OrbwalkerType.AfterAttack)
        //    {
        //        AAPassive = Player.HasBuff("TwitchPassiveBuff");
        //        return;
        //    }
        //}

        //private static void OnBasicAttack(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        //{
        //    if (sender.IsMe)
        //    {
        //        AAPassive = false;
        //        return;
        //    }
        //}
        private static void OnMenuLoad()
        {
            Menu = new Menu("Twitch", "Ryusung.Twitch", true);
            Notifications.Add(new Notification("Dao Hung AIO", "Twitch credit Hoola and Fuck you WWapper"));
            var Combo = new Menu("Combo", "Combo");
            Combo.Add(new MenuBool("CQ", "Use Q"));
            Combo.Add(new MenuBool("CW", "Use W"));
            Combo.Add(new MenuList("CE", "Use E Mode", new[] { "Side", "Cursor", "Enemy", "Never" })).Permashow();
            Combo.Add(new MenuKeyBind("ForceR", "Force R On Target Selector", System.Windows.Forms.Keys.T, KeyBindType.Press)).Permashow();
            Menu.Add(Combo);

            var Misc = new Menu("Misc", "Misc");
            Misc.Add(new MenuSlider("Humanizer", "Humanizer Delay", 5, 0, 300));
            Misc.Add(new MenuBool("Nocolision", "Nocolision W"));
            Misc.Add(new MenuBool("SkipPassive", "Fast combo skip some passive?", false)).Permashow();
            Menu.Add(Misc);


            var Harass = new Menu("Harass", "Harass");
            Harass.Add(new MenuBool("HEXQ", "Use Extended Q"));
            Harass.Add(new MenuSlider("HMinMana", "Extended Q Min Mana (%)", 80));
            Harass.Add(new MenuBool("HQ", "Use Q"));
            Harass.Add(new MenuBool("HW", "Use W"));
            Harass.Add(new MenuList("HE", "Use E Mode", new[] { "Side", "Cursor", "Enemy", "Never" })).Permashow();
            Harass.Add(new MenuBool("HHMinMana", "Harass Min Mana (%)"));
            Menu.Add(Harass);

            var LC = new Menu("LaneClear", "LaneClear");
            LC.Add(new MenuKeyBind("LT", "Use Spell LaneClear (Toggle)", System.Windows.Forms.Keys.J, KeyBindType.Toggle)).Permashow();
            LC.Add(new MenuBool("LHQ", "Use Extended Q For Harass"));
            LC.Add(new MenuSlider("LQ", "Use Q (0 = Don't)", 0, 0, 5));
            LC.Add(new MenuBool("LW", "Use W"));
            LC.Add(new MenuBool("LE", "Use E"));
            LC.Add(new MenuSlider("LMinMana", "Min Mana (%)", 80, 0, 100));
            Menu.Add(LC);

            var JC = new Menu("JungleClear", "JungleClear");
            JC.Add(new MenuBool("JQ", "Use Q"));
            JC.Add(new MenuBool("JW", "Use W"));
            JC.Add(new MenuBool("JE", "Use E"));
            Menu.Add(JC);

            var Auto = new Menu("Auto", "Auto");
            Auto.Add(new MenuKeyBind("AutoQ", "Auto Extended Q (Toggle)", System.Windows.Forms.Keys.A, KeyBindType.Toggle)).Permashow();
            Auto.Add(new MenuSlider("MinMana", "Min Mana (%)", 80));
            Menu.Add(Auto);

            var Draw = new Menu("Draw", "Draw");
            Draw.Add(new MenuBool("Dind", "Draw Damage Incidator"));
            Draw.Add(new MenuBool("DEQ", "Draw Extended Q")).SetValue(false);
            Draw.Add(new MenuBool("DQ", "Draw Q")).SetValue(false);
            Draw.Add(new MenuBool("DW", "Draw W")).SetValue(false);
            Draw.Add(new MenuBool("DE", "Draw E")).SetValue(false);
            Menu.Add(Draw);

            var killsteal = new Menu("killsteal", "Killsteal");
            killsteal.Add(new MenuBool("KillstealQ", "Killsteal Q"));
            Menu.Add(killsteal);

            Menu.Add(new Menu("Creadit", "Creadit: Hoola"));

            Menu.Attach();
        }

        private static void OnBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if (sender.IsMe)
            {
                AAPassive = Player.HasBuff("TwitchPassiveBuff");
            }
        }
        private static void OnBuffGain(AIBaseClient sender, AIBaseClientBuffLoseEventArgs args)
        {
            if (sender.IsMe)
            {
                AAPassive = Player.HasBuff("TwitchPassiveBuff");
            }
        }
        private static void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var spellName = args.SData.Name;
            if (!sender.IsMe || !Orbwalker.IsAutoAttack(spellName)) return;

            if (args.Target is AIHeroClient)
            {
                var target = (AIBaseClient)args.Target;
                if (Orbwalker.ActiveMode == OrbwalkerMode.Harass && target.IsValid)
                {
                    Utility.DelayAction.Add(Humanizer, () => OnDoCastDelayed(args));
                }
            }
            if (args.Target is AIMinionClient)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && args.Target.IsValid)
                {
                    Utility.DelayAction.Add(Humanizer, () => OnDoCastDelayed(args));
                }
            }
        }
        private static void OnDoCastLC(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var spellName = args.SData.Name;
            if (!sender.IsMe || !Orbwalker.IsAutoAttack(spellName)) return;

            if (args.Target is AIMinionClient)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && args.Target.IsValid)
                {
                    Utility.DelayAction.Add(Humanizer, () => OnDoCastDelayedLC(args));
                }
            }
        }

        static void killsteal()
        {
            if (KillstealQ && Q.IsReady())
            {
                var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) && !x.IsDead);
                foreach (var target in targets)
                {
                    if (target.Health < Q.GetDamage(target) && (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") && !target.HasBuff("JudicatorIntervention")))
                        Q.Cast(target);
                }
            }
        }
        private static void OnDoCastDelayedLC(AIBaseClientProcessSpellCastEventArgs args)
        {
            if (SP)
            {
                AAPassive = false;
            }
            else
            {
                AAPassive = Player.HasBuff("TwitchPassiveBuff");
            }
            if (args.Target is AIMinionClient && args.Target.IsValid)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Player.ManaPercent > LMinMana)
                {
                    var Minions = GameObjects.GetMinions(Player.GetRealAutoAttackRange(), MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                    if (Minions[0].IsValid && Minions.Count != 0)
                    {
                        if (!LT) return;

                        if (E.IsReady() && !AAPassive && LE) E.Cast(Player.Position.Extend(Game.CursorPos, 70));
                        if (Q.IsReady() && (!E.IsReady() || (E.IsReady() && !LE)) && LQ != 0 && !AAPassive)
                        {
                            var QMinions = GameObjects.GetMinions(Q.Range);
                            var exminions = GameObjects.GetMinions(Q1.Range);
                            foreach (var Minion in QMinions)
                            {
                                var QHit = new Geometry.Rectangle(Player.Position, Player.Position.Extend(Minion.Position, Q1.Range), Q1.Width);
                                if (exminions.Count(x => !QHit.IsOutside(x.Position.ToVector2())) >= LQ)
                                {
                                    Q.Cast(Minion);
                                    break;
                                }
                            }
                        }
                        if ((!E.IsReady() || (E.IsReady() && !LE)) && (!Q.IsReady() || (Q.IsReady() && LQ == 0)) && LW && W.IsReady() && !AAPassive) W.Cast(Minions[0].Position);
                    }
                }
            }
        }
        public static Vector2 Deviation(Vector2 point1, Vector2 point2, double angle)
        {
            angle *= Math.PI / 180.0;
            Vector2 temp = Vector2.Subtract(point2, point1);
            Vector2 result = new Vector2(0);
            result.X = (float)(temp.X * Math.Cos(angle) - temp.Y * Math.Sin(angle)) / 4;
            result.Y = (float)(temp.X * Math.Sin(angle) + temp.Y * Math.Cos(angle)) / 4;
            result = Vector2.Add(result, point1);
            return result;
        }
        private static void OnDoCastDelayed(AIBaseClientProcessSpellCastEventArgs args)
        {

            if (SP)
            {
                AAPassive = false;
            }
            else
            {
                AAPassive = Player.HasBuff("TwitchPassiveBuff");
            }
            if (args.Target is AIHeroClient)
            {
                var target = (AIBaseClient)args.Target;
                if (Orbwalker.ActiveMode == OrbwalkerMode.Harass && target.IsValid)
                {
                    if (Player.ManaPercent < HHMinMana) return;

                    if (E.IsReady() && !AAPassive && HE == "Side") E.Cast((Deviation(Player.Position.ToVector2(), target.Position.ToVector2(), 65).ToVector3()));
                    if (E.IsReady() && !AAPassive && HE == "Cursor") E.Cast(Player.Position.Extend(Game.CursorPos, 50));
                    if (E.IsReady() && !AAPassive && HE == "Enemy") E.Cast(Player.Position.Extend(target.Position, 50));
                    if (Q.IsReady() && (!E.IsReady() || (E.IsReady() && HE == "Never")) && HQ && !AAPassive) Q.Cast(target);
                    if ((!E.IsReady() || (E.IsReady() && HE == "Never")) && (!Q.IsReady() || (Q.IsReady() && !HQ)) && HW && W.IsReady() && !AAPassive) W.Cast(target.Position);
                }
            }
            if (args.Target is AIMinionClient && args.Target.IsValid)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    var Mobs = GameObjects.GetMinions(Player.GetRealAutoAttackRange(), MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                    if (Mobs[0].IsValid && Mobs.Count != 0)
                    {
                        if (E.IsReady() && !AAPassive && JE) E.Cast(Player.Position.Extend(Game.CursorPos, 70));
                        if (Q.IsReady() && (!E.IsReady() || (E.IsReady() && !JE)) && JQ && !AAPassive) Q.Cast(Mobs[0]);
                        if ((!E.IsReady() || (E.IsReady() && !JE)) && (!Q.IsReady() || (Q.IsReady() && !JQ)) && JW && W.IsReady() && !AAPassive) W.Cast(Mobs[0].Position);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (Player.ManaPercent < HMinMana) return;

            if (Q.IsReady() && HEXQ)
            {
                var target = TargetSelector.GetTarget(Q1.Range);
                var Minions = GameObjects.GetMinions(Q.Range);
                foreach (var Minion in Minions)
                {
                    var QHit = new Geometry.Rectangle(Player.Position, Player.Position.Extend(Minion.Position, Q1.Range), Q1.Width);
                    var QPred = Q1.GetPrediction(target);
                    if (!QHit.IsOutside(QPred.UnitPosition.ToVector2()) && QPred.Hitchance == HitChance.High)
                    {
                        Q.Cast(Minion);
                        break;
                    }
                }
            }
        }
        static void LaneClear()
        {
            if (Player.ManaPercent < LMinMana) return;

            if (Q.IsReady() && LHQ)
            {
                var extarget = TargetSelector.GetTarget(Q1.Range);
                var Minions = GameObjects.GetMinions(Q.Range);
                foreach (var Minion in Minions)
                {
                    var QHit = new Geometry.Rectangle(Player.Position, Player.Position.Extend(Minion.Position, Q1.Range), Q1.Width);
                    var QPred = Q1.GetSPrediction(extarget);
                    if (!QHit.IsOutside(QPred.UnitPosition) && QPred.HitChance == HitChance.High)
                    {
                        Q.Cast(Minion);
                        break;
                    }
                }
            }
        }
        static void AutoUseQ()
        {
            if (Q.IsReady() && AutoQ && Player.ManaPercent > MinMana)
            {
                var extarget = TargetSelector.GetTarget(Q1.Range);
                var Minions = GameObjects.GetMinions(Q.Range);
                foreach (var Minion in Minions)
                {
                    var QHit = new Geometry.Rectangle(Player.Position, Player.Position.Extend(Minion.Position, Q1.Range), Q1.Width);
                    var QPred = Q1.GetSPrediction(extarget);
                    if (!QHit.IsOutside(QPred.UnitPosition) && QPred.HitChance == HitChance.High)
                    {
                        Q.Cast(Minion);
                        break;
                    }
                }
            }
        }

        static void UseRTarget()
        {
            var target = TargetSelector.GetTarget(R.Range);
            if (target != null && ForceR && R.IsReady() && target.IsValid && target is AIHeroClient && !Player.HasBuff("TwitchR")) R.Cast(target.Position);
        }

        static void Combo()
        {
            if (SP)
            {
                AAPassive = false;
            }
            else
            {
                AAPassive = Player.HasBuff("TwitchPassiveBuff");
            }
            var target = Orbwalker.GetTarget();

            if (target is AIHeroClient)
            {// E menuList "Side", "Cursor", "Enemy", "Never" 
                target = (AIBaseClient)target;
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && target.IsValid)
                {
                    if (Player.CanUseItem((int)ItemId.Youmuus_Ghostblade)) Player.UseItem((int)ItemId.Youmuus_Ghostblade);
                    if (E.IsReady() && !AAPassive)
                    {
                        if (CE == "Side")
                            Utility.DelayAction.Add((int)Math.Ceiling(Player.AttackCastDelay * 1500), () => {
                                E.Cast((Deviation(Player.Position.ToVector2(), target.Position.ToVector2(), 65).ToVector3()));
                            }
                            );
                        if (CE == "Cursor")
                            Utility.DelayAction.Add((int)Math.Ceiling(Player.AttackCastDelay * 1000), () => {
                                E.Cast(Game.CursorPos);
                            });
                        if (CE == "Enemy")
                            Utility.DelayAction.Add((int)Math.Ceiling(Player.AttackCastDelay * 1000), () => {
                                E.Cast(Player.Position.Extend(target.Position, 50));
                            });
                        return;
                    }
                    if (Q.IsReady() && (!E.IsReady() || (E.IsReady() && CE == "Never")) && CQ && !AAPassive)
                    {
                        Q.Cast((AIBaseClient)target);
                        return;
                    }
                    if ((!E.IsReady() || (E.IsReady() && CE == "Never")) && (!Q.IsReady() || (Q.IsReady() && !CQ)) && CW && W.IsReady() && !AAPassive)
                    {
                        W.Cast(target.Position);
                        return;
                    }
                }
            }
        }
        static void Game_OnUpdate(EventArgs args)
        {
            W.Collision = Menu["Misc"].GetValue<MenuBool>("Nocolision");
            AutoUseQ();
            if (ForceR) UseRTarget();
            killsteal();
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo) Combo();
            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass) Harass();
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear) LaneClear();
        }
        static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E) AAPassive = true;
            if (args.Slot == SpellSlot.E) Orbwalker.ResetAutoAttackTimer();
            if (args.Slot == SpellSlot.R && Player.CanUseItem((int)ItemId.Youmuus_Ghostblade)) Player.UseItem((int)ItemId.Youmuus_Ghostblade);
        }

        static float getComboDamage(AIBaseClient enemy)
        {
            if (enemy != null)
            {
                float damage = 0;
                if (E.IsReady()) damage = damage + (float)Player.GetAutoAttackDamage(enemy) * 2;
                if (W.IsReady()) damage = damage + W.GetDamage(enemy) + (float)Player.GetAutoAttackDamage(enemy);
                if (Q.IsReady())
                {
                    damage = damage + Q.GetDamage(enemy) + (float)Player.GetAutoAttackDamage(enemy);
                }
                damage = damage + (float)Player.GetAutoAttackDamage(enemy);

                return damage;
            }
            return 0;
        }

        static void OnDraw(EventArgs args)
        {
            if (DEQ) Render.Circle.DrawCircle(Player.Position, Q1.Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            if (DQ) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            if (DW) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
            if (DE) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
        }
        //static void Drawing_OnEndScene(EventArgs args)
        //{
        //    if (Dind)
        //    {
        //        foreach (
        //            var enemy in
        //                ObjectManager.Get<AIHeroClient>()
        //                    .Where(ene => ene.IsValidTarget() && !ene.IsDead))
        //        {
        //            Indicator.unit = enemy;
        //            Indicator.drawDmg(getComboDamage(enemy), new ColorBGRA(255, 204, 0, 160));

        //        }
        //    }
        //}
    }
}
