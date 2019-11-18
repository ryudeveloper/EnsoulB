using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keys = System.Windows.Forms.Keys;
using EnsoulSharp;
using EnsoulSharp.SDK;
using Utility = EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;
using SPrediction;

namespace vMiqueiasAIO.Champions
{
    class RivenReborn
    {
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        private static string R1name = "RivenFengShuiEngine";

        private static string R2name = "RivenIzunaBlade";


        private static Spell Q, W, E, R;

        private static SpellSlot flash = Player.GetSpellSlot("summonerflash");

        private static Menu Menu;

        public static bool waitE, waitQ, waitAA, waitW, waitTiamat, waitR1, waitR2, midAA, canAA, forceQ, forceW, forceT, forceR, waitR, castR, forceEburst, qGap
            , R2style;
        public static int waitQTick, waitR2Tick;
        private static AttackableUnit TTTar = null;

        public static float cE, cQ, cAA, cW, cTiamt, cR1, cR2, Wind, countforce, Rstate, R2countdonw;
        public static int Qstate = 1;
        //private static int Windup { get { return Orbwalker("ExtraWindup").Value; } }


        public RivenReborn()
        {

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 250);
            R = new Spell(SpellSlot.R, 900);
            R.SetSkillshot(0.25f, 45, 1600, false, false, SkillshotType.Cone);
            R.MinHitChance = HitChance.Medium;

            Menu = new Menu("DH." + Player.CharacterName, "DH." + Player.CharacterName, true);
            Menu.Add(new MenuKeyBind("Burst", "Burst", Keys.T, KeyBindType.Press));
            Menu.Add(new MenuKeyBind("FastHarass", "FastHarass", Keys.V, KeyBindType.Press));
            //Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Menu spellMenu = Menu.Add(new Menu("Spells", "Spells"));
            spellMenu.Add(new MenuBool("RcomboAlways", "RcomboAlways", false));
            spellMenu.Add(new MenuBool("RcomboKillable", "RcomboKillable"));
            spellMenu.Add(new MenuBool("R2comboKS", "R2comboKS"));
            spellMenu.Add(new MenuBool("R2comboMaxdmg", "RcomboMaxdmg"));
            //spellMenu.Add(new MenuBool("R2BadaoStyle", "R2 Badao Style"));
            spellMenu.Add(new MenuBool("Ecombo", "Ecombo"));
            spellMenu.Add(new MenuBool("QGap", "Q Gap", false));
            spellMenu.Add(new MenuBool("UseQBeforeExpiry", "Use Q Before Expiry"));
            spellMenu.Add(new MenuBool("QstrangeCancel", "Q strange Cancel"));
            spellMenu.Add(new MenuList("Qmode", "Q cast mode", new[] { "Lock Target", "To Mouse" }, 0));
            Menu BurstCombo = spellMenu.Add(new Menu("BurstCombo", "Burst Combo"));
            //BurstCombo.Add(new MenuBool("Burst", "Burst").SetValue(new KeyBind('T', KeyBindType.Press)));
            BurstCombo.Add(new MenuBool("UseFlash", "Use Flash", false));
            Menu Misc = Menu.Add(new Menu("Misc", "Misc"));
            Misc.Add(new MenuBool("Winterrupt", "W interrupt"));
            Misc.Add(new MenuBool("Wgapcloser", "W gapcloser"));
            Menu Draw = Menu.Add(new Menu("Draw", "Draw"));
            Draw.Add(new MenuBool("Drawdmgtext", "Draw dmg text"));
            Menu other = Menu.Add(new Menu("Other", "Other"));
            other.Add(new MenuKeyBind("Flee", "Flee", Keys.Z, KeyBindType.Press));
            other.Add(new MenuKeyBind("WallJumpHelper", "WallJumpHelper", Keys.A, KeyBindType.Press));
            //other.Add(new MenuBool("FastHarass", "FastHarass").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Press)));
            Menu Clear = Menu.Add(new Menu("Clear", "Clear"));
            Clear.Add(new MenuBool("UseTiamat", "Use Tiamat"));
            Clear.Add(new MenuBool("UseQ", "Use Q"));
            Clear.Add(new MenuBool("UseW", "Use W"));
            Clear.Add(new MenuBool("UseE", "Use E"));
            Menu.Attach();

            Drawing.OnDraw += OnDraw;

            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Orbwalker.OnAction += OnActionDelegate;
            AIBaseClient.OnProcessSpellCast += oncast;
            AIBaseClient.OnPlayAnimation += AIBaseClient_OnPlayAnimation;
            Interrupter.OnInterrupterSpell += interrupt;
            Gapcloser.OnGapcloser += gapcloser;

        }

        private static void OnActionDelegate(
            Object sender,
            OrbwalkerActionArgs args
        )
        {
            var target = args.Target;
            if (args.Type == OrbwalkerType.AfterAttack)
            {
                TTTar = target;
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (HasItem())
                    {
                        CastItem();
                    }
                    //else if (R2BadaoStyle && R.IsReady() && R.Instance.Name == R2name && Qstate == 3)
                    //{
                    //    if (target is AIBaseClient)
                    //    {
                    //        R.Cast(target as AIBaseClient);
                    //    }
                    //    if (Q.IsReady())
                    //    {
                    //        Utility.DelayAction.Add(150, () => callbackQ(TTTar));
                    //    }
                    //}
                    else if (W.IsReady() && InWRange(target))
                    {
                        W.Cast();
                        if (Q.IsReady())
                        {
                            Utility.DelayAction.Add(150, () => callbackQ(TTTar));
                        }
                    }
                    else if (Q.IsReady())
                    {
                        callbackQ(TTTar);
                    }
                    else if (E.IsReady() && Ecombo)
                    {
                        E.Cast(target.Position);
                    }
                }
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    if (HasItem() && UseTiamatClear)
                    {
                        CastItem();
                    }
                    else if (W.IsReady() && InWRange(target) && UseWClear)
                    {
                        W.Cast();
                        if (Q.IsReady() && UseQClear)
                        {
                            Utility.DelayAction.Add(150, () => callbackQ(TTTar));
                        }
                    }
                    else if (Q.IsReady() && UseQClear)
                    {
                        callbackQ(TTTar);
                    }
                    else if (E.IsReady() && UseEClear)
                    {
                        E.Cast(target.Position);
                    }
                }
                if (Menu.GetValue<MenuKeyBind>("Burst").Active)
                {
                    if (HasItem())
                    {
                        CastItem();
                        if (R.IsReady() && R.Instance.Name == R2name)
                        {
                            if (target is AIHeroClient)
                            {
                                callbackR2(TTTar);
                            }
                            if (Q.IsReady())
                            {
                                Utility.DelayAction.Add(150, () => callbackQ(TTTar));
                            }
                        }
                        else if (Q.IsReady())
                        {
                            callbackQ(TTTar);
                        }

                    }
                    else if (R.IsReady() && R.Instance.Name == R2name)
                    {
                        if (target is AIHeroClient)
                        {
                            R.Cast(target as AIHeroClient);
                        }
                        if (Q.IsReady())
                        {
                            Utility.DelayAction.Add(150, () => callbackQ(TTTar));
                        }
                    }
                    else if (W.IsReady() && InWRange(target))
                    {
                        W.Cast();
                        if (Q.IsReady())
                        {
                            Utility.DelayAction.Add(150, () => callbackQ(TTTar));
                        }
                    }
                    else if (Q.IsReady())
                    {
                        callbackQ(TTTar);
                    }
                    else if (E.IsReady() && Ecombo)
                    {
                        E.Cast(target.Position);
                    }
                }
                if (Menu.GetValue<MenuKeyBind>("FastHarass").Active)
                {
                    if (HasItem())
                    {
                        CastItem();
                    }
                    else if (W.IsReady() && InWRange(target))
                    {
                        W.Cast();
                        if (Q.IsReady())
                        {
                            Utility.DelayAction.Add(150, () => callbackQ(TTTar));
                        }
                    }
                    else if (Q.IsReady())
                    {
                        Q.Cast(target.Position);
                    }
                    else if (E.IsReady())
                    {
                        E.Cast(target.Position);
                    }
                }
            }
            if(args.Type == OrbwalkerType.OnAttack)
            {
                if (args.Sender.IsMe && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (Player.CanUseItem((int)ItemId.Youmuus_Ghostblade))
                        Player.UseItem((int)ItemId.Youmuus_Ghostblade);
                }
            }

        }
        private static void AIBaseClient_OnPlayAnimation(
    AIBaseClient sender,
    AIBaseClientPlayAnimationEventArgs args
)
        {
            if (!sender.IsMe)
                return;

            //Game.Print(args.Animation);
            if (args.Animation.Contains("1a"))
            {
                //Game.Print("Cancel Q1");
                //if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                //{
                //Utility.DelayAction.Add(200 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                //Utility.DelayAction.Add(240 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                Utility.DelayAction.Add(280 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                //}
                Qstate = 2;
            }
            else if (args.Animation.Contains("1b"))
            {
                //Game.Print("Cancel Q2");
                //if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                //Utility.DelayAction.Add(220 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                //Utility.DelayAction.Add(260 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                Utility.DelayAction.Add(300 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                Qstate = 3;
            }
            else if (args.Animation.Contains("1c"))
            {
                //Game.Print("Cancel Q3");
                //if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                //Utility.DelayAction.Add(300 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                //Utility.DelayAction.Add(340 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                Utility.DelayAction.Add(380 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10)));
                Qstate = 1;
            }

        }

        private static string Qmode { get { return Menu["Spells"].GetValue<MenuList>("Qmode").SelectedValue; } }
        private static bool Qstrangecancel { get { return Menu["Spells"].GetValue<MenuBool>("QstrangeCancel"); } }
        private static bool Rcomboalways { get { return Menu["Spells"].GetValue<MenuBool>("RcomboAlways"); } }
        private static bool RcomboKillable { get { return Menu["Spells"].GetValue<MenuBool>("RcomboKillable"); } }
        private static bool R2comboKS { get { return Menu["Spells"].GetValue<MenuBool>("R2comboKS"); } }
        private static bool R2comboMaxdmg { get { return Menu["Spells"].GetValue<MenuBool>("R2comboMaxdmg"); } }
        //private static bool R2BadaoStyle { get { return Menu["Spells"].GetValue<MenuBool>("R2BadaoStyle"); } }
        private static bool Ecombo { get { return Menu["Spells"].GetValue<MenuBool>("Ecombo"); } }
        private static bool QGap { get { return Menu["Spells"].GetValue<MenuBool>("QGap"); } }
        private static bool UseQBeforeExpiry { get { return Menu["Spells"].GetValue<MenuBool>("UseQBeforeExpiry"); } }
        private static bool BurstActive { get { return Menu.GetValue<MenuKeyBind>("Burst").Active; } }
        private static bool FlashBurst { get { return Menu["Spells"]["BurstCombo"].GetValue<MenuBool>("UseFlash"); } }
        private static bool Winterrupt { get { return Menu["Misc"].GetValue<MenuBool>("Winterrupt"); } }
        private static bool Wgapcloser { get { return Menu["Misc"].GetValue<MenuBool>("Wgapcloser"); } }

        private static bool Drawdamage { get { return Menu["Draw"].GetValue<MenuBool>("Drawdmgtext"); } }
        private static bool FleeActive { get { return Menu["Other"].GetValue<MenuKeyBind>("Flee").Active; } }
        private static bool WallJumpHelperActive { get { return Menu["Other"].GetValue<MenuKeyBind>("WallJumpHelper").Active; } }
        private static bool FastHarassActive { get { return Menu.GetValue<MenuKeyBind>("FastHarass").Active; } }
        private static bool UseTiamatClear { get { return Menu["Clear"].GetValue<MenuBool>("UseTiamat"); } }
        private static bool UseQClear { get { return Menu["Clear"].GetValue<MenuBool>("UseQ"); } }
        private static bool UseWClear { get { return Menu["Clear"].GetValue<MenuBool>("UseW"); } }
        private static bool UseEClear { get { return Menu["Clear"].GetValue<MenuBool>("UseE"); } }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            SolvingWaitList();
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                Clear();
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                Combo();
            if (Menu.GetValue<MenuKeyBind>("Burst").Active)
                Burst();
            if (Menu.GetValue<MenuKeyBind>("FastHarass").Active)
                fastharass();
            if (WallJumpHelperActive)
                walljump();
            if (FleeActive)
                flee();
        }
        public static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsDead)
                Render.Circle.DrawCircle(target.Position, 150, Color.AliceBlue, 15);
            if (Menu["Draw"].GetValue<MenuBool>("Drawdmgtext"))
                foreach (var hero in GameObjects.EnemyHeroes)
                {
                    if (hero.IsValidTarget(1500))
                    {
                        var dmg = totaldame(hero) > hero.Health ? 100 : totaldame(hero) * 100 / hero.Health;
                        var dmg1 = Math.Round(dmg);
                        var x = Drawing.WorldToScreen(hero.Position);
                        Color mau = dmg1 == 100 ? Color.Red : Color.Yellow;
                        Drawing.DrawText(x[0], x[1], mau, dmg1.ToString() + " %");
                    }
                }
        }
        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
           
        }
        public static void interrupt(
    AIHeroClient sender,
    Interrupter.InterruptSpellArgs args
)
        {
            if (sender.IsEnemy && W.IsReady() && sender.IsValidTarget() && !sender.IsDead && Winterrupt)
            {
                if (sender.IsValidTarget(125 + Player.BoundingRadius + sender.BoundingRadius)) W.Cast();
            }
        }
        public static void gapcloser(
    AIHeroClient sender,
    Gapcloser.GapcloserArgs args
)
        {
            var target = sender;
            if (target.IsEnemy && W.IsReady() && target.IsValidTarget() && !target.IsDead && Wgapcloser)
            {
                if (target.IsValidTarget(125 + Player.BoundingRadius + target.BoundingRadius)) W.Cast();
            }
        }
        private static void oncast(
    AIBaseClient sender,
    AIBaseClientProcessSpellCastEventArgs args
)
        {
            var spell = args.SData;

            if (!sender.IsMe)
            {
                return;
            }
            if (spell.Name.Contains("ItemTiamatCleave"))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Menu.GetValue<MenuKeyBind>("FastHarass").Active)
                {
                    if (Q.IsReady())
                    {
                        callbackQ(TTTar);
                    }
                }
            }
            if (Orbwalker.IsAutoAttack(args.SData.Name))
            {

            }
            if (spell.Name.Contains("RivenTriCleave"))
            {

                waitQ = false;
                Orbwalker.ResetAutoAttackTimer();
                if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Utility.DelayAction.Add(40, () => Reset(40));
                }

                cQ = Variables.GameTimeTickCount;
            }
            if (spell.Name.Contains("RivenMartyr"))
            {
                Utility.DelayAction.Add(160 - Game.Ping, () => Cancel());//Chat.Say("/d Fuck Wapper", false));

            }
            if (spell.Name.Contains("RivenFient"))
            {

                if (Menu.GetValue<MenuKeyBind>("Burst").Active)
                {
                    if (R.IsReady() && R.Instance.Name == R1name)
                        Utility.DelayAction.Add(150, () => R.Cast());
                }
            }
            if (spell.Name.Contains("RivenFengShuiEngine"))
            {
                Utility.DelayAction.Add(140 - Game.Ping, () => Cancel());//Chat.Say("/d Fuck Wapper", false));

            }
            if (spell.Name.Contains("rivenizunablade"))
            {
                Utility.DelayAction.Add(140 - Game.Ping, () => Cancel());//Chat.Say("/d Fuck Wapper", false));

            }
        }

        private static void Reset(int t)
        {
            Utility.DelayAction.Add(0, () => Orbwalker.ResetAutoAttackTimer());
            for (int i = 10; i < t; i = i + 10)
            {
                if (i - Game.Ping >= 0)
                    Utility.DelayAction.Add(i - Game.Ping, () => Cancel());
            }
        }
        private static void Cancel()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 500));
            if (Qstrangecancel) Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10));
        }
        private static void OnAttack(AttackableUnit unit, AttackableUnit target)
        {

        }

        private static void Burst()
        {
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsDead)
            {
                if (target.InAutoAttackRange() && Orbwalker.CanMove() && (!R.IsReady() || (R.IsReady() && R.Instance.Name == R1name)))
                {
                    W.Cast();
                    return;
                }
                if (target.InAutoAttackRange() && Orbwalker.CanMove() && R.IsReady())
                {
                    if (R.IsReady() && R.Instance.Name == R1name) R.Cast();
                    Utility.DelayAction.Add(350, () => CastItem());
                    Utility.DelayAction.Add(400, () => W.Cast());
                    return;
                }
                if (!target.InAutoAttackRange() && Orbwalker.CanMove() && E.IsReady() && R.IsReady() && Player.Distance(target.Position) <= E.Range + Player.BoundingRadius + target.BoundingRadius)
                {
                    E.Cast(Player.Position.Extend(target.Position, 200));
                    if (R.IsReady() && R.Instance.Name == R1name) R.Cast();
                    Utility.DelayAction.Add(350, () => CastItem());
                    Utility.DelayAction.Add(400, () => W.Cast());
                    return;
                }
                if (!target.InAutoAttackRange() && Orbwalker.CanMove() && !E.IsReady() && R.IsReady() && !Player.IsDashing()
                    && flash != SpellSlot.Unknown && flash.IsReady() && FlashBurst && Player.Distance(target.Position) <= 425 + Player.BoundingRadius + target.BoundingRadius)
                {
                    if (R.IsReady() && R.Instance.Name == R1name) R.Cast();
                    var x = Player.Distance(target.Position) > 425 ? Player.Position.Extend(target.Position, 425) : target.Position;
                    Player.Spellbook.CastSpell(flash, x);
                    Utility.DelayAction.Add(350, () => CastItem());
                    Utility.DelayAction.Add(400, () => W.Cast());
                    return;
                }
                if (!target.InAutoAttackRange() && Orbwalker.CanMove() && E.IsReady() && flash != SpellSlot.Unknown && flash.IsReady() && FlashBurst
                    && R.IsReady() && Player.Distance(target.Position) <= E.Range + Player.BoundingRadius + target.BoundingRadius + 425
                    && Player.Distance(target.Position) > Player.BoundingRadius + target.BoundingRadius + 425)
                {
                    if (R.IsReady() && R.Instance.Name == R1name) R.Cast();
                    E.Cast(Player.Position.Extend(target.Position, 200));
                    Utility.DelayAction.Add(350, () => Player.Spellbook.CastSpell(flash, target.Position));
                    Utility.DelayAction.Add(350, () => CastItem());
                    Utility.DelayAction.Add(500, () => W.Cast());
                    return;
                }
            }
        }

        private static void Combo()
        {
            if (Q.IsReady() && Orbwalker.CanMove() && QGap && !Player.IsDashing())
            {
                var target = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget()).OrderByDescending(x => 1 - x.Distance(Player.Position)).FirstOrDefault();
                if (!Player.IsDashing() && Variables.GameTimeTickCount - cQ >= 1000 && target.IsValidTarget())
                {
                    if (Prediction.GetFastUnitPosition(Player, 100).Distance(target.Position) <= Player.Distance(target.Position))
                        Q.Cast(Game.CursorPos);
                    return;
                }
            }
            if (W.IsReady() && Orbwalker.CanMove())
            {
                var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && !x.IsDead && InWRange(x));
                if (targets.Any())
                {
                    W.Cast();
                    return;
                }
            }
            if (E.IsReady() && Orbwalker.CanMove() && Ecombo)
            {
                var target = TargetSelector.GetTarget(325 + Player.AttackRange + 70);
                if (target.IsValidTarget() && !target.IsDead)
                {
                    E.Cast(target.Position);
                    return;
                }
            }
            if (R.IsReady())
            {
                if (R.Instance.Name == R1name)
                {
                    if (Rcomboalways)
                    {
                        var target = TargetSelector.GetTarget(325 + Player.AttackRange + 70);
                        if (target.IsValidTarget() && !target.IsDead && E.IsReady())
                        {
                            R.Cast();
                            return;
                        }
                        else
                        {
                            var targetR = TargetSelector.GetTarget(200 + Player.BoundingRadius + 70);
                            if (targetR.IsValidTarget() && !targetR.IsDead)
                            {
                                R.Cast();
                                return;
                            }
                        }

                    }
                    if (RcomboKillable)
                    {
                        var targetR = TargetSelector.GetTarget(200 + Player.BoundingRadius + 70);
                        if (targetR.IsValidTarget() && !targetR.IsDead && basicdmg(targetR) <= targetR.Health && totaldame(targetR) >= targetR.Health)
                        {
                            R.Cast();
                            return;
                        }
                        if (targetR.IsValidTarget() && !targetR.IsDead && Player.CountEnemyHeroesInRange(800) >= 2)
                        {
                            R.Cast();
                            return;
                        }
                    }
                }
                else if (R.Instance.Name == R2name)
                {
                    if (R2comboKS)
                    {
                        var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && !x.IsDead);
                        foreach (var target in targets)
                        {
                            if (target.Health < Rdame(target, target.Health))
                                R.Cast(target);
                            return;
                        }
                    }
                    if (R2comboMaxdmg)
                    {
                        var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && !x.IsDead);
                        foreach (var target in targets)
                        {
                            if (target.Health / target.MaxHealth <= 0.25)
                                R.Cast(target);
                            return;
                        }
                    }
                    //if (R2BadaoStyle && !Q.IsReady())
                    //{
                    //    var target = TargetSelector.GetTarget(R.Range);
                    //    if (target.IsValidTarget() && !target.IsDead)
                    //    {
                    //        R.Cast(target);
                    //        return;
                    //    }
                    //}
                    var targethits = TargetSelector.GetTarget(R.Range);
                    if (targethits.IsValidTarget() && !targethits.IsDead)
                        R.CastIfWillHit(targethits, 4);
                    return;

                }
            }
        }
        private static void Clear()
        {
            var targetW = GameObjects.GetMinions(Player.Position, WRange() + 100, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
            var targetW2 = GameObjects.Jungle.Where(x => x.IsValidTarget(WRange() + 100)).OrderBy(x => x.MaxHealth).ToList<AIBaseClient>().FirstOrDefault();
            if (targetW != null && InWRange(targetW) && W.IsReady() && Orbwalker.CanMove() && UseWClear)
            {
                W.Cast();
                return;
            }
            if (targetW2 != null && InWRange(targetW2) && W.IsReady() && Orbwalker.CanMove() && UseWClear)
            {
                W.Cast();
                return;
            }
            if (targetW != null && InWRange(targetW) && E.IsReady() && Orbwalker.CanMove() && UseEClear)
            {
                E.Cast(targetW.Position);
                return;
            }
            if (targetW2 != null && InWRange(targetW2) && E.IsReady() && Orbwalker.CanMove() && UseEClear)
            {
                E.Cast(targetW2.Position);
                return;
            }
        }
        public static void fastharass()
        {
            if (W.IsReady() && Orbwalker.CanMove())
            {
                var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && !x.IsDead && InWRange(x));
                if (targets.Any())
                {
                    W.Cast();
                    return;
                }
            }
            if (E.IsReady() && Orbwalker.CanMove())
            {
                var target = TargetSelector.GetTarget(325 + Player.AttackRange + 70);
                if (target.IsValidTarget() && !target.IsDead)
                {
                    E.Cast(target.Position);
                    return;
                }
            }
        }
        private static void SolvingWaitList()
        {
            if (!Q.IsReady(1000)) Qstate = 1;
            if (waitQ == true && TTTar.IsValidTarget())
            {
                //if (Variables.GameTimeTickCount - cQ >= 350 + Player.AttackCastDelay - Game.Ping / 2) //"Lock Target", "To Mouse" 
                if (Orbwalker.ActiveMode != OrbwalkerMode.LaneClear)
                {
                    if (Qmode == "Lock Target")

                        Q.Cast(Prediction.GetFastUnitPosition((AIBaseClient)TTTar, 300));
                    else
                    {
                        Q.Cast(Game.CursorPos);
                    }
                        
                    return;
                }
                else
                {
                    if (Qmode == "Lock Target")
                        Q.Cast(Prediction.GetFastUnitPosition((AIBaseClient)TTTar, 300));
                    else
                    {
                        Q.Cast(Game.CursorPos);
                    }
                    return;
                }
                if (Environment.TickCount - waitQTick >= 500 + Game.Ping / 2)
                    waitQ = false;
            }
            if (waitR2 == true && TTTar.IsValidTarget())
            {
                if (Environment.TickCount - waitQTick >= 500 + Game.Ping / 2)
                    waitQ = false;
                R.Cast(TTTar as AIBaseClient);
                return;
            }
            if (Q.IsReady() && UseQBeforeExpiry && !Player.IsRecalling())
            {
                if (Qstate != 1 && Variables.GameTimeTickCount - cQ <= 3800 - Game.Ping / 2 && Variables.GameTimeTickCount - cQ >= 3300 - Game.Ping / 2) { Q.Cast(Game.CursorPos);
                    return;
                }
            }
        }
        public static bool HasItem()
        {
            if (Player.CanUseItem((int)ItemId.Youmuus_Ghostblade) || Player.CanUseItem((int)ItemId.Tiamat_Melee_Only) || Player.CanUseItem((int)ItemId.Ravenous_Hydra_Melee_Only) || Player.CanUseItem((int)ItemId.Titanic_Hydra) || Player.CanUseItem((int)ItemId.Ravenous_Hydra))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void CastItem()
        {

            if (Player.CanUseItem((int)ItemId.Youmuus_Ghostblade))
                Player.UseItem((int)ItemId.Youmuus_Ghostblade);
            if (Player.CanUseItem((int)ItemId.Ravenous_Hydra_Melee_Only))
                Player.UseItem((int)ItemId.Ravenous_Hydra_Melee_Only);
            if (Player.CanUseItem((int)ItemId.Titanic_Hydra))
                Player.UseItem((int)ItemId.Titanic_Hydra);
            if (Player.CanUseItem((int)ItemId.Ravenous_Hydra))
                Player.UseItem((int)ItemId.Ravenous_Hydra);
            if (Player.CanUseItem((int)ItemId.Tiamat_Melee_Only))
                Player.UseItem((int)ItemId.Tiamat_Melee_Only);
        }

        private static bool InWRange(AttackableUnit target)
        {
            if (Player.HasBuff("RivenFengShuiEngine"))
            {
                return
                    target.BoundingRadius + 200 + Player.BoundingRadius >= Player.Distance(target.Position);
            }
            else
            {
                return
                   target.BoundingRadius + 125 + Player.BoundingRadius >= Player.Distance(target.Position);
            }
        }
        private static float WRange()
        {
            if (Player.HasBuff("RivenFengShuiEngine"))
            {
                return
                    200 + Player.BoundingRadius;
            }
            else
            {
                return
                   125 + Player.BoundingRadius;
            }
        }
        private static void callbackQ(AttackableUnit target)
        {
            waitQ = true;
            TTTar = target;
            waitQTick = Environment.TickCount;
        }
        private static void callbackR2(AttackableUnit target)
        {
            waitR2 = true;
            TTTar = target;
            waitR2Tick = Environment.TickCount;
        }
        public static void checkbuff()
        {
            String temp = "";
            foreach (var buff in Player.Buffs)
            {
                temp += (buff.Name + "(" + buff.Count + ")" + "(" + buff.Type.ToString() + ")" + ", ");
            }
            Game.Print(temp);
        }
        public static double basicdmg(AIBaseClient target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5; }
                else if (Player.Level >= 15) { passivenhan = 0.45; }
                else if (Player.Level >= 12) { passivenhan = 0.4; }
                else if (Player.Level >= 9) { passivenhan = 0.35; }
                else if (Player.Level >= 6) { passivenhan = 0.3; }
                else if (Player.Level >= 3) { passivenhan = 0.25; }
                else { passivenhan = 0.2; }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage(target) * 0.7;
                if (W.IsReady()) dmg = dmg + W.GetDamage(target);
                if (Q.IsReady())
                {
                    var qnhan = 4 - Qstate;
                    dmg = dmg + Q.GetDamage(target) * qnhan + Player.GetAutoAttackDamage(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage(target) * (1 + passivenhan);
                return dmg;
            }
            else { return 0; }
        }
        public static double totaldame(AIBaseClient target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5; }
                else if (Player.Level >= 15) { passivenhan = 0.45; }
                else if (Player.Level >= 12) { passivenhan = 0.4; }
                else if (Player.Level >= 9) { passivenhan = 0.35; }
                else if (Player.Level >= 6) { passivenhan = 0.3; }
                else if (Player.Level >= 3) { passivenhan = 0.25; }
                else { passivenhan = 0.2; }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage(target) * 0.7;
                if (W.IsReady()) dmg = dmg + W.GetDamage(target);
                if (Q.IsReady())
                {
                    var qnhan = 4 - Qstate;
                    dmg = dmg + Q.GetDamage(target) * qnhan + Player.GetAutoAttackDamage(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage(target) * (1 + passivenhan);
                if (R.IsReady())
                {
                    if (Rstate == 0)
                    {
                        var rdmg = Rdame(target, target.Health - dmg * 1.2);
                        return dmg * 1.2 + rdmg;
                    }
                    else if (Rstate == 1)
                    {
                        var rdmg = Rdame(target, target.Health - dmg);
                        return rdmg + dmg;
                    }
                    else return dmg;
                }
                else return dmg;
            }
            else return 0;
        }
        public static double Rdame(AIBaseClient target, double health)
        {
            if (target != null)
            {
                //var missinghealth = (target.MaxHealth - health) / target.MaxHealth > 0.75 ? 0.75 : (target.MaxHealth - health) / target.MaxHealth;
                //var pluspercent = missinghealth * (8 / 3);
                //var rawdmg = new double[] { 80, 150, 160 }[R.Level - 1] + 0.6 * Player.FlatPhysicalDamageMod;
                
                //Game.Print(R.GetDamage(target));
                return R.GetDamage(target);
            }
            else return 0;
        }

        public static void walljump()
        {
            var x = Player.Position.Extend(Game.CursorPos, 100);
            var y = Player.Position.Extend(Game.CursorPos, 30);
            if (!x.IsWall() && !y.IsWall()) Player.IssueOrder(GameObjectOrder.MoveTo, x);
            if (x.IsWall() && !y.IsWall()) Player.IssueOrder(GameObjectOrder.MoveTo, y);
            if (Prediction.GetFastUnitPosition(Player, 500).Distance(Player.Position) <= 10) { Q.Cast(Game.CursorPos); }
        }
        public static void flee()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            var x = Player.Position.Extend(Game.CursorPos, 300);
            if (Q.IsReady() && !Player.IsDashing()) Q.Cast(Game.CursorPos);
            if (E.IsReady() && !Player.IsDashing()) E.Cast(x);
        }
    }
}
