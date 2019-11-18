using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using SharpDX;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI.Values;
using Keys = System.Windows.Forms.Keys;
using SPrediction;
using RyusungAIO.Helpers;
using EnsoulSharp.SDK.Events;

namespace RyusungAIO.Champions
{
    class Nautilus
    {
        private static readonly AIHeroClient Player = ObjectManager.Player;

        public static Spell Q { get; set; }
        public static Spell W { get; set; }
        public static Spell E { get; set; }
        public static Spell R { get; set; }

        private static SpellSlot _smiteSlot = SpellSlot.Unknown;
        private static Spell _smite;

        //credits Kurisu
        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };


        private static SpellSlot _ignite;

        private static Menu _menu;


        public Nautilus()
        {


            Q = new Spell(SpellSlot.Q, 1100);
            Q.SetSkillshot(250, 90, 2000, true, SkillshotType.Line);
            W = new Spell(SpellSlot.W, 175);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 825);

            SettingupSmite();
            _ignite = Player.GetSpellSlot("summonerdot");

            _menu = new Menu(Player.CharacterName, "RY." + Player.CharacterName + "Credits Kyon",  true);


            Menu combo = _menu.AddSubMenu(new Menu("combo", "combo"));
            {
                combo.Add(new MenuBool("CombouseQ", "Use Q").SetValue(true)); //y
                combo.Add(new MenuBool("CombouseW", "Use W").SetValue(true)); //y
                combo.Add(new MenuBool("CombouseE", "Use E").SetValue(true)); //y
                combo.Add(new MenuBool("CombouseR", "Use R").SetValue(true)); //y
                combo.Add(new MenuBool("CombouseSmite", "Use Smite").SetValue(true)); //y
                combo.Add(new MenuBool("CombouseIgnite", "Use Ignite").SetValue(true)); //y
            }

            var usageR = _menu.AddSubMenu((new Menu("Ult Settings", "Ultwork")));
            {
                foreach (var target in ObjectManager.Get<AIHeroClient>().Where(target => target.IsEnemy))
                    usageR.Add(new MenuBool("DontR" + target.CharacterName, target.CharacterName).SetValue(false));
            }

            Menu killsteal = _menu.AddSubMenu(new Menu("killsteal", "Killsteal"));
            {
                killsteal.Add(new MenuBool("ksinuse", "Killsteal").SetValue(true));
                killsteal.Add(new MenuBool("ksQ", "Use Q").SetValue(true)); //y
                killsteal.Add(new MenuBool("ksE", "Use E").SetValue(true)); //y

            }

            Menu laneclear = _menu.AddSubMenu(new Menu("laneclear", "laneclear"));
            {
                laneclear.Add(new MenuBool("laneuseQ", "Use Q").SetValue(true)); //y
                laneclear.Add(new MenuBool("laneuseW", "Use W").SetValue(true)); //y
                laneclear.Add(new MenuBool("laneuseE", "Use E").SetValue(true)); //y
                laneclear.Add(new MenuSlider("laneE", "when x minions").SetValue(new Slider(3, 1, 10)));
                laneclear.Add(new MenuSlider("laneuntilmana", "min mana in %").SetValue(new Slider(25)));
            }

            Menu flee = _menu.AddSubMenu(new Menu("flee", "flee"));
            {
                flee.Add(new MenuKeyBind("fleekey", "flee ! ", Keys.A, KeyBindType.Press)); //A
                flee.Add(new MenuBool("fleeuseQ", "Use Q").SetValue(true)); //y
                flee.Add(new MenuBool("fleeuseW", "Use W").SetValue(true)); //y
                flee.Add(new MenuBool("fleeusewalls", "use walls").SetValue(true)); //y
                flee.Add(new MenuBool("fleeuseminions", "use minions").SetValue(true)); //y
            }

            Menu drawings = _menu.AddSubMenu(new Menu("drawings", "drawings"));
            {
                drawings.Add(new MenuBool("drawingsdrawQ", "Draw Q").SetValue(true)); //y
                drawings.Add(new MenuBool("drawingsdrawW", "Draw W").SetValue(true)); //y
                drawings.Add(new MenuBool("drawingsdrawE", "Draw E").SetValue(true)); //y
                drawings.Add(new MenuBool("drawingsdrawR", "Draw R").SetValue(true)); //y
            }

            Menu misc = _menu.AddSubMenu(new Menu("misc", "misc"));
            {
                misc.Add(new MenuBool("miscigniteuse", "Use Ignite").SetValue(true)); //y
            }

            _menu.Attach();
            Interrupter.OnInterrupterSpell += Interrupter2OnOnInterruptableTarget;
            Drawing.OnDraw += Drawing_OnDraw;
            Tick.OnTick += Game_OnUpdate;
        }

        private static void SettingupSmite()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => String.Equals(spell.Name, Smitetype(), StringComparison.CurrentCultureIgnoreCase)))
            {
                _smiteSlot = spell.Slot;
                _smite = new Spell(_smiteSlot, 700);
                return;
            }
        }

        public static string Smitetype()
        {
            if (SmiteBlue.Any(id => Player.HasItem(id)))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(id => Player.HasItem(id)))
            {
                return "s5_summonersmiteduel";
            }
            if (SmiteGrey.Any(id => Player.HasItem(id)))
            {
                return "s5_summonersmitequick";
            }
            if (SmitePurple.Any(id => Player.HasItem(id)))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }

        private static void Interrupter2OnOnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (sender.IsEnemy && sender.Distance(Player) <= Q.Range && args.DangerLevel == Interrupter.DangerLevel.High || args.DangerLevel == Interrupter.DangerLevel.Medium)
            {
                var hitchance = Q.GetPrediction(sender, false, 0,                    
                        CollisionObjects.Heroes |
                        CollisionObjects.Minions |
                        CollisionObjects.Walls |
                        CollisionObjects.YasuoWall).Hitchance;

                if (hitchance == HitChance.VeryHigh || hitchance == HitChance.High || hitchance == HitChance.Immobile ||
                    hitchance == HitChance.Dash)
                {
                    Q.Cast(sender);
                }
            }
            else if (sender.IsEnemy && sender.InAutoAttackRange() && args.DangerLevel == Interrupter.DangerLevel.High || args.DangerLevel == Interrupter.DangerLevel.Medium)
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
            }

        }

 

        private static void Game_OnUpdate(EventArgs args)
        {
            if (_menu.Item("fleekey").GetValue<MenuKeyBind>().Active)
            {
                Flee();
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.LaneClear:
                    Laneclear();
                    break;
            }

            Killsteal();
        }

        private static int CalcDamage(AIBaseClient target)
        {
            var aa = Player.GetAutoAttackDamage(target) * (1 + Player.Crit);
            var damage = aa;
            _ignite = Player.GetSpellSlot("summonerdot");

            if (_ignite.IsReady())
                damage += Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);

            if (R.IsReady()) // rdamage
            {
                damage += R.GetDamage(target);
            }

            if (Q.IsReady()) // qdamage
            {

                damage += Q.GetDamage(target);
            }

            if (E.IsReady()) // edamage
            {

                damage += E.GetDamage(target);
            }

            if (_smite.IsReady()) // edamage
            {

                damage += GetSmiteDmg();
            }

            return (int)damage;
        }

        private static int GetSmiteDmg()
        {
            int level = Player.Level;
            int index = Player.Level / 5;
            float[] dmgs = { 370 + 20 * level, 330 + 30 * level, 240 + 40 * level, 100 + 50 * level };
            return (int)dmgs[index];
        }

        private static void Laneclear()
        {

            var minion = GameObjects.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.All);

            if (Player.ManaPercent <= _menu.Item("laneuntilmana").GetValue<MenuSlider>().Value)
                return;

            if (minion.Count >= _menu.Item("laneE").GetValue<MenuSlider>().Value && E.IsReady() && minion.First().IsValidTarget(E.Range - 50) && _menu.Item("laneuseE").GetValue<MenuBool>())
            {
                E.Cast(true);
            }

            if (Q.IsReady() && _menu.Item("laneuseQ").GetValue<MenuBool>())
            {
                var jungleMobs = GameObjects.GetJungles(Q.Range, JungleType.All, JungleOrderTypes.MaxHealth);
                if (jungleMobs.Count >= 3)
                {
                    var target = jungleMobs.First();
                    Q.CastIfHitchanceEquals(target, HitChance.Medium, true);
                }
            }

            if (W.IsReady() && Player.HealthPercent <= 75 && _menu.Item("laneuseE").GetValue<MenuBool>())
            {
                W.Cast(true);
            }

        }

        private static void Combo()
        {
            bool vQ = Q.IsReady() && _menu.Item("CombouseQ").GetValue<MenuBool>();
            bool vW = W.IsReady() && _menu.Item("CombouseW").GetValue<MenuBool>();
            bool vE = E.IsReady() && _menu.Item("CombouseE").GetValue<MenuBool>();
            bool vR = R.IsReady() && _menu.Item("CombouseR").GetValue<MenuBool>();
            bool ign = _ignite.IsReady() && _menu.Item("CombouseIgnite").GetValue<MenuBool>();

            var tsQ = TargetSelector.GetTarget(Q.Range);
            var tsR = TargetSelector.GetTarget(R.Range);

            if (tsQ == null || tsR == null)
                return;

            if (vR && tsR.IsValidTarget(R.Range) && tsR.Health > R.GetDamage(tsR))
            {
                var useR = (_menu.Item("DontR" + tsR.CharacterName) != null &&
                           _menu.Item("DontR" + tsR.CharacterName).GetValue<MenuBool>() == false);
                if (useR)
                {
                    R.CastOnUnit(tsR);
                }
            }

            UseSmite(tsQ);

            if (vQ && tsQ.IsValidTarget())
            {
                var qpred = Q.GetPrediction(tsQ);
                if (qpred.CollisionObjects.Count(c => c.IsEnemy && !c.IsDead) < 4 && qpred.Hitchance >= HitChance.High)
                {
                    Q.Cast(tsQ);
                }
            }

            if (vW && tsQ.IsValidTarget(W.Range))
                W.Cast();

            if (vE && tsQ.IsValidTarget(E.Range))
                E.Cast();

            if (Player.Distance(tsQ.Position) <= 600 && IgniteDamage(tsQ) >= tsQ.Health && ign)
                Player.Spellbook.CastSpell(_ignite, tsQ);

        }

        private static float IgniteDamage(AIHeroClient target)
        {
            if (_ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(_ignite) != SpellState.Ready)
                return 0f;
            return (float)Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
        }
        //thanks Justy
        private static void Killsteal()
        {
            if (!_menu.Item("ksinuse").GetValue<MenuBool>())
                return;

            foreach (AIHeroClient target in
                ObjectManager.Get<AIHeroClient>()
                    .Where(
                        hero =>
                            hero.IsValidTarget(Q.Range) && !hero.HasBuffOfType(BuffType.Invulnerability) && hero.IsEnemy)
                )
            {
                var qDmg = Player.GetSpellDamage(target, SpellSlot.Q);
                if (_menu.Item("ksQ").GetValue<MenuBool>() && target.IsValidTarget(Q.Range) && target.Health <= qDmg)
                {
                    var qpred = Q.GetPrediction(target);
                    if (qpred.Hitchance >= HitChance.High && qpred.CollisionObjects.Count(h => h.IsEnemy && !h.IsDead && h is AIMinionClient) < 2)
                        Q.Cast(qpred.CastPosition);
                }
                var eDmg = Player.GetSpellDamage(target, SpellSlot.E);
                if (_menu.Item("ksE").GetValue<MenuBool>() && target.IsValidTarget(E.Range) && target.Health <= eDmg)
                {
                    E.Cast();
                }
            }
        }
        //Credits to metaphorce
        public static void UseSmite(AIHeroClient target)
        {
            var usesmite = _menu.Item("CombouseSmite").GetValue<MenuBool>();
            var itemscheck = SmiteBlue.Any(i => Player.HasItem(i)) || SmiteRed.Any(i => Player.HasItem(i));
            if (itemscheck && usesmite &&
                ObjectManager.Player.Spellbook.CanUseSpell(_smiteSlot) == SpellState.Ready &&
                target.Distance(Player.Position) < _smite.Range)
            {
                ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, target);
            }
        }

        private static void Flee()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos, false);

            bool vQ = Q.IsReady() && _menu.Item("fleeuseQ").GetValue<MenuBool>();
            bool vW = W.IsReady() && _menu.Item("fleeuseW").GetValue<MenuBool>();

            var minions = ObjectManager.Get<AIMinionClient>().Where(minion => minion.IsValidTarget(Q.Range)).ToList(); // Hopefully this is enough...
            var step = Q.Range / 2; // Or whatever step value...
            for (var i = step; i <= Q.Range; i += step)
            {
                if (ObjectManager.Player.Position.Extend(Game.CursorPos, i).IsWall() && Player.Distance(Game.CursorPos) >= Q.Range / 2 && vQ)
                {
                    Q.Cast(Game.CursorPos);
                }

                var target =
                    minions.FirstOrDefault(
                     minion =>
                       Player.Position.ToVector2().CircleCircleIntersection(
                                minion.Position.ToVector2(),
                                Q.Range,
                                minion.BoundingRadius).Count() > 0);

                if (target != null && target.Distance(Player.Position) >= Q.Range / 2 && vQ)
                {
                    Q.Cast(target.Position);
                }
            }

            if (vW && ObjectManager.Get<AIBaseClient>().Any(x => x.IsEnemy && x.Distance(Player.Position) <= Q.Range && Player.IsTargetable))
            {
                W.Cast();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            if (Q.IsReady() && _menu.Item("drawingsdrawQ").GetValue<MenuBool>())
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Color.Crimson);
            }

            if (W.IsReady() && _menu.Item("drawingsdrawW").GetValue<MenuBool>())
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, Color.CornflowerBlue);
            }

            if (E.IsReady() && _menu.Item("drawingsdrawE").GetValue<MenuBool>())
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, Color.FloralWhite);
            }

            if (R.IsReady() && _menu.Item("drawingsdrawR").GetValue<MenuBool>())
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, Color.Orange);
            }

        }
    }
}
