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
using Utility = EnsoulSharp.SDK.Utility;


namespace RyusungAIO.Champions
{
    class Leesin
    {
        private const string CharacterName = "LeeSin";
        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static Menu _config;
        public static Menu TargetSelectorMenu;
        private static AIHeroClient _player;
        private static AIBaseClient insobj;
        private static SpellSlot _igniteSlot;
        private static SpellSlot _flashSlot;
        private static SpellSlot _smitedmgSlot;
        private static SpellSlot _smitehpSlot;
        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis, _youmuu;
        public static Vector3 WardCastPosition;
        private static Vector3 insdirec;
        private static Vector3 insecpos;
        private static Vector3 movepoint;
        private static Vector3 jumppoint;
        private static Vector3 wpos;
        private static Vector3 wallcheck;
        private static Vector3 firstpos;
        private static int canmove = 1;
        private static int instypecheck;
        private static float wardtime;
        private static float inscount;
        private static float counttime;
        private static float qcasttime;
        private static float q2casttime;
        private static float wcasttime;
        private static float ecasttime;
        private static float casttime;
        private static bool walljump;
        private static bool checker;
        private static int bCount;
        private static bool castQAgain;


        public Leesin()
        {
            try
            {
                _player = ObjectManager.Player;

                _q = new Spell(SpellSlot.Q, 1100f);
                _w = new Spell(SpellSlot.W, 700f);
                _e = new Spell(SpellSlot.E, 330f);
                _r = new Spell(SpellSlot.R, 375f);

                _q.SetSkillshot(0.25f, 65f, 1800f, true, SkillshotType.Line);

                _bilge = new Items.Item(3144, 475f);
                _blade = new Items.Item(3153, 425f);
                _hydra = new Items.Item(3074, 250f);
                _tiamat = new Items.Item(3077, 250f);
                _rand = new Items.Item(3143, 490f);
                _lotis = new Items.Item(3190, 590f);
                _youmuu = new Items.Item(3142, 10);

                _igniteSlot = _player.GetSpellSlot("SummonerDot");
                _flashSlot = _player.GetSpellSlot("SummonerFlash");
                _smitedmgSlot = _player.GetSpellSlot(SmitetypeDmg());
                _smitehpSlot = _player.GetSpellSlot(SmitetypeHp());




                _config = new Menu("LeeIsBack", "Ryusung.Lee credits JackisSharp, ChewyMoon, JQuery", true);



                _config.AddSubMenu(new Menu("Combo", "Combo Lee is back"));
                _config.SubMenu("Combo").Add(new MenuKeyBind("ActiveCombo", "Combo!", Keys.Space, KeyBindType.Press));
                _config.SubMenu("Combo").Add(new MenuBool("UseIgnitecombo", "Use Ignite(rush for it)")).SetValue(true);
                _config.SubMenu("Combo").Add(new MenuBool("UseSmitecombo", "Use Smite(rush for it)")).SetValue(true);
                _config.SubMenu("Combo").Add(new MenuBool("UseWcombo", "Use W")).SetValue(false);


                _config.AddSubMenu(new Menu("Combo2", "Combo ElLeesin"));
                _config.SubMenu("Combo2").Add(new MenuKeyBind("ActiveCombo2", "Combo!", Keys.T, KeyBindType.Press));
                _config.SubMenu("Combo2").Add(new MenuBool("ElLeeSin.Combo.Q", "Use Q").SetValue(true));
                _config.SubMenu("Combo2").Add(new MenuBool("qSmite", "Smite Q!").SetValue(false));
                _config.SubMenu("Combo2").Add(new MenuBool("ElLeeSin.Combo.Q2", "Use Q2").SetValue(true));
                _config.SubMenu("Combo2").Add(new MenuBool("ElLeeSin.Combo.W2", "Use W").SetValue(true));
                _config.SubMenu("Combo2").Add(new MenuBool("ElLeeSin.Combo.E", "Use E").SetValue(true));
                _config.SubMenu("Combo2").Add(new MenuBool("ElLeeSin.Combo.R", "Use R").SetValue(true));
                _config.SubMenu("Combo2").Add(new MenuSlider("ElLeeSin.Combo.PassiveStacks", "Min Stacks").SetValue(new Slider(1, 1, 2)));

                _config.SubMenu("Combo2").Add(new Menu("Wardjump", "Wardjump"));
                _config.SubMenu("Combo2")
                    .SubMenu("Wardjump")
                    .Add(new MenuBool("ElLeeSin.Combo.W", "Wardjump in combo").SetValue(false));
                _config.SubMenu("Combo2")
                    .SubMenu("Wardjump")
                    .Add(new MenuBool("ElLeeSin.Combo.Mode.WW", "Out of AA range").SetValue(false));

                _config.SubMenu("Combo2").Add(new MenuBool("ElLeeSin.Combo.KS.R", "KS R").SetValue(true));
                _config.SubMenu("Combo2")
                    .Add(
                        new MenuKeyBind("starCombo", "Star Combo", Keys.U, KeyBindType.Press));

                _config.SubMenu("Combo2").Add(new MenuBool("ElLeeSin.Combo.AAStacks", "Wait for Passive").SetValue(false));

                _config.AddSubMenu(new Menu("Insec", "Insec"));
                _config.SubMenu("Insec").Add(new MenuKeyBind("insc", "insec active", Keys.A, KeyBindType.Press));
                _config.SubMenu("Insec").Add(new MenuBool("minins", "insec using minions?")).SetValue(false);
                _config.SubMenu("Insec").Add(new MenuBool("fins", "flash if no wards")).SetValue(true);

                _config.AddSubMenu(new Menu("Harass", "Harass"));
                _config.SubMenu("Harass")
                    .Add(
                        new MenuKeyBind("ActiveHarass", "Harass!", Keys.C, KeyBindType.Press));
                _config.SubMenu("Harass").Add(new MenuBool("UseItemsharass", "Use Tiamat/Hydra")).SetValue(true);
                _config.SubMenu("Harass").Add(new MenuBool("UseEHar", "Use E")).SetValue(true);
                _config.SubMenu("Harass").Add(new MenuBool("UseQ1Har", "Use Q1 Harass")).SetValue(true);
                _config.SubMenu("Harass").Add(new MenuBool("UseQ2Har", "Use Q2 Harass")).SetValue(true);


                _config.AddSubMenu(new Menu("items", "items"));
                _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
                _config.SubMenu("items").SubMenu("Offensive").Add(new MenuBool("Youmuu", "Use Youmuu's")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").Add(new MenuBool("Tiamat", "Use Tiamat")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").Add(new MenuBool("Hydra", "Use Hydra")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").Add(new MenuBool("Bilge", "Use Bilge")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .Add(new MenuSlider("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .Add(new MenuSlider("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").SubMenu("Offensive").Add(new MenuBool("Blade", "Use Blade")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .Add(new MenuSlider("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .Add(new MenuSlider("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").AddSubMenu(new Menu("Deffensive", "Deffensive"));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .Add(new MenuBool("Omen", "Use Randuin Omen"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .Add(new MenuSlider("Omenenemys", "Randuin if enemys>").SetValue(new Slider(2, 1, 5)));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .Add(new MenuBool("lotis", "Use Iron Solari"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .Add(new MenuSlider("lotisminhp", "Solari if Ally Hp<").SetValue(new Slider(35, 1, 100)));

                //Farm
                _config.AddSubMenu(new Menu("Farm", "Farm"));
                _config.SubMenu("Farm").AddSubMenu(new Menu("LaneFarm", "LaneFarm"));
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .Add(new MenuBool("UseItemslane", "Use Hydra/Tiamat"))
                    .SetValue(true);
                _config.SubMenu("Farm").SubMenu("LaneFarm").Add(new MenuBool("UseQL", "Q LaneClear")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("LaneFarm").Add(new MenuBool("UseEL", "E LaneClear")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .Add(
                        new MenuKeyBind("Activelane", "Lane clear!", Keys.S, KeyBindType.Press));

                _config.SubMenu("Farm").AddSubMenu(new Menu("LastHit", "LastHit"));
                _config.SubMenu("Farm").SubMenu("LastHit").Add(new MenuBool("UseQLH", "Q LastHit")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("LastHit")
                    .Add(
                        new MenuKeyBind("Activelast", "LastHit!", Keys.X, KeyBindType.Press));

                _config.SubMenu("Farm").AddSubMenu(new Menu("Jungle", "Jungle"));
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .Add(new MenuBool("UseItemsjungle", "Use Hydra/Tiamat"))
                    .SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").Add(new MenuBool("UseQJ", "Q Jungle")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").Add(new MenuBool("UseWJ", "W Jungle")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").Add(new MenuBool("UseEJ", "E Jungle")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").Add(new MenuBool("PriW", "W>E? (off E>W)")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .Add(
                        new MenuKeyBind("Activejungle", "Jungle!", Keys.S, KeyBindType.Press));

                //Misc
                _config.AddSubMenu(new Menu("Misc", "Misc"));
                _config.SubMenu("Misc").Add(new MenuBool("UseIgnitekill", "Use Ignite KillSteal")).SetValue(true);
                _config.SubMenu("Misc").Add(new MenuBool("UseEM", "Use E KillSteal")).SetValue(true);
                _config.SubMenu("Misc").Add(new MenuBool("UseRM", "Use R KillSteal")).SetValue(true);
                _config.SubMenu("Misc").Add(new MenuKeyBind("wjump", "ward jump", Keys.Z, KeyBindType.Press));
                _config.SubMenu("Misc").Add(new MenuBool("wjmax", "ward jump max range?")).SetValue(false);





                //Drawings
                _config.AddSubMenu(new Menu("Drawings", "Drawings"));
                _config.SubMenu("Drawings").Add(new MenuBool("DrawQ", "Draw Q")).SetValue(true);
                _config.SubMenu("Drawings").Add(new MenuBool("DrawE", "Draw E")).SetValue(true);
                _config.SubMenu("Drawings").Add(new MenuBool("DrawR", "Draw R")).SetValue(true);
                _config.SubMenu("Drawings").Add(new MenuBool("damagetest", "Damage Text")).SetValue(true);
                _config.SubMenu("Drawings").Add(new MenuBool("CircleLag", "Lag Free Circles").SetValue(true));
                _config.SubMenu("Drawings")
                    .Add(new MenuSlider("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
                _config.SubMenu("Drawings")
                    .Add(new MenuSlider("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));
                _config.Attach();

                Drawing.OnDraw += Drawing_OnDraw;
                EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnUpdate;
                AIBaseClient.OnProcessSpellCast += OnProcessSpell;
                //AIBaseClient.OnBuffGain += BuffGain;
                Game.OnWndProc += OnWndProc;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.Print("Error something went wrong");
            }
        }

        //private void BuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        //{
        //    Game.Print(args.Buff.Name + ':' + args.Buff.Count);
        //}

        private static void Game_OnUpdate(EventArgs args)
        {
            try
            {
                if (_config.Item("ActiveCombo").GetValue<MenuKeyBind>().Active)
                {
                    Combo(GetEnemy);

                }
                if (_config.Item("ActiveCombo2").GetValue<MenuKeyBind>().Active || _config.Item("starCombo").GetValue<MenuKeyBind>().Active)
                {
                    Combo2();

                }

                if (_config.Item("wjump").GetValue<MenuKeyBind>().Active)
                {
                    wjumpflee();
                }
                if (_config.Item("ActiveHarass").GetValue<MenuKeyBind>().Active)
                {
                    Harass(GetEnemy);

                }
                if (_config.Item("insc").GetValue<MenuKeyBind>().Active)
                {
                    Orbwalker.Move(Game.CursorPos);
                    Insec(GetEnemy);

                }
                if (_config.Item("Activejungle").GetValue<MenuKeyBind>().Active)
                {
                    JungleClear();
                }
                if (_config.Item("Activelane").GetValue<MenuKeyBind>().Active)
                {
                    LaneClear();
                }
                if (_config.Item("Activelast").GetValue<MenuKeyBind>().Active)
                {
                    LastHit();
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message, false);

                Console.WriteLine(e.StackTrace, false);
            }




        }

        private static void OnWndProc(GameWndProcEventArgs args)
        {
            if (args.Msg == 515 || args.Msg == 513)
            {
                if (args.Msg == 515)
                {
                    insdirec = Game.CursorPos;
                    instypecheck = 1;
                }
                var boohoo = ObjectManager.Get<AIBaseClient>()
                         .OrderBy(obj => obj.Distance(_player.Position))
                         .FirstOrDefault(
                             obj =>
                                 obj.IsAlly && !obj.IsMe && !obj.IsMinion &&
                                  Game.CursorPos.Distance(obj.Position) <= 150);

                if (args.Msg == 513 && boohoo != null)
                {
                    insobj = boohoo;
                    instypecheck = 2;
                }
            }

        }

        private static void OnProcessSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                casttime = Environment.TickCount;
            }

            if (args.SData.Name == "BlindMonkQOne")
            {
                castQAgain = false;
                Utility.DelayAction.Add(2900, () => { castQAgain = true; });
            }
            if (sender.IsAlly || !sender.Type.Equals(GameObjectType.AIHeroClient) ||
                (((AIHeroClient)sender).CharacterName != "MonkeyKing" && ((AIHeroClient)sender).CharacterName != "Akali") ||
                sender.Position.Distance(_player.Position) >= 330 ||
                !_e.IsReady())
            {
                return;
            }
            if (args.SData.Name == "MonkeyKingDecoy" || args.SData.Name == "AkaliSmokeBomb")
            {
                _e.Cast();
            }
        }
        private static float ComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;
            if (_igniteSlot != SpellSlot.Unknown && _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            if (_smitedmgSlot != SpellSlot.Unknown && _player.Spellbook.CanUseSpell(_smitedmgSlot) == SpellState.Ready)
                damage += 20 + 8 * _player.Level;
            if (_player.HasItem(3077) && _player.CanUseItem(3077))
                damage += _player.GetItemDamage(enemy, "Tiamat");
            if (_player.HasItem(3074) && _player.CanUseItem(3074))
                damage += _player.GetItemDamage(enemy, "Ravenous_Hydra");
            if (_player.HasItem(3074) && _player.CanUseItem(3748))
                damage += _player.GetItemDamage(enemy, "Titanic_Hydra");
            if (_player.HasItem(3153) && _player.CanUseItem(3153))
                damage += _player.GetItemDamage(enemy, "Blade_of_the_Ruined_King");
            if (_player.HasItem(3144) && _player.CanUseItem(3144))
                damage += _player.GetItemDamage(enemy, "Bilgewater_Cutlass");
            if (QStage == QCastStage.First)
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q) * 2;
            if (EStage == ECastStage.First)
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (_r.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage;
        }
        private static bool Passive()
        {
            if (_player.HasBuff("blindmonkpassive_cosmetic"))
            {
                return true;
            }
            else
                return false;
        }

        private static void Combo(AIHeroClient t)
        {

            if (t == null)
            {
                return;
            }
            if (_config.Item("UseIgnitecombo").GetValue<MenuBool>() && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (ComboDamage(t) > t.Health)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, t);
                }
            }
            if (_config.Item("UseWcombo").GetValue<MenuBool>() && t.Distance(_player.Position) <= _player.GetRealAutoAttackRange())
            {
                if (WStage == WCastStage.First || !Passive())
                    CastSelfW();
                if (WStage == WCastStage.Second && (!Passive() || Environment.TickCount > wcasttime + 2500))
                    _w.Cast();
            }
            if (_config.Item("UseSmitecombo").GetValue<MenuBool>() && _smitedmgSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_smitedmgSlot) == SpellState.Ready)
            {
                if (ComboDamage(t) > t.Health)
                {
                    _player.Spellbook.CastSpell(_smitedmgSlot, t);
                }
            }

            if (t.IsValidTarget() && _q.IsReady() && t.Distance(_player.Position) < 1100)
            {
                CastQ1(t);
            }

            if (t.HasBuff("BlindMonkQOne") || t.HasBuff("blindmonkqonechaos") && (ComboDamage(t) > t.Health || t.Distance(_player.Position) > 350 || Environment.TickCount > qcasttime + 2500))
                _q.Cast();


            CastECombo();
            UseItemes(t);

        }
        public static bool ParamBool(string paramName)
        {
            return _config.Item(paramName).GetValue<MenuBool>();
        }
        private static void Combo2(){

            var target = TargetSelector.GetTarget(_q.Range);
            if (!target.IsValidTarget() || target == null)
            {
                return;
            }

            UseItemes(target);

            if (target.HasQBuff() && ParamBool("ElLeeSin.Combo.Q2"))
            {
                if (castQAgain
                    || target.HasBuffOfType(BuffType.Knockback) && !_player.IsValidTarget(300)
                    && !_r.IsReady() || !target.IsValidTarget(_player.GetRealAutoAttackRange())
                    || _q.GetDamage(target) > target.Health
                    || ReturnQBuff().Distance(target) < _player.Distance(target)
                    && !target.IsValidTarget(_player.GetRealAutoAttackRange()))
                {
                    _q.Cast(target);
                }
            }

            if (_r.GetDamage(target) >= target.Health && ParamBool("ElLeeSin.Combo.KS.R")
                && target.IsValidTarget())
            {
                _r.Cast(target);
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks")
                && PassiveStacks > _config.Item("ElLeeSin.Combo.PassiveStacks").GetValue<MenuSlider>().Value
                && _player.GetRealAutoAttackRange() > _player.Distance(target))
            {
                return;
            }

            if (ParamBool("ElLeeSin.Combo.W"))
            {
                if (ParamBool("ElLeeSin.Combo.Mode.WW")
                    && target.Distance(_player) > _player.GetRealAutoAttackRange())
                {
                    WardJump(target.Position, false, true);
                }

                if (!ParamBool("ElLeeSin.Combo.Mode.WW") && target.Distance(_player) > _q.Range)
                {
                    WardJump(target.Position, false, true);
                }
            }

            if (_e.IsReady() && ParamBool("ElLeeSin.Combo.E"))
            {
                if (EState && target.Distance(_player) < _e.Range)
                {
                    _e.Cast();
                    return;
                }

                if (!EState && target.Distance(_player) > _player.GetRealAutoAttackRange() + 50)
                {
                    _e.Cast();
                }
            }

            if (_q.IsReady() && _q.Instance.Name == "BlindMonkQOne"
                && ParamBool("ElLeeSin.Combo.Q"))
            {
                CastQ(target, ParamBool("qSmite"));
            }

            if (_r.IsReady() && _q.IsReady() && target.HasQBuff()
                && ParamBool("ElLeeSin.Combo.R"))
            {
                _r.CastOnUnit(target);
            }
        }

        private static AIBaseClient ReturnQBuff()
        {
            return
                ObjectManager.Get<AIBaseClient>()
                    .Where(a => a.IsValidTarget(1300))
                    .FirstOrDefault(unit => unit.HasQBuff());
        }

        public static bool EState
        {
            get
            {
                return _e.Instance.Name == "BlindMonkEOne";
            }
        }
        private static void CastQ(AIBaseClient target, bool smiteQ = false)
        {
            if (!_q.IsReady() || !target.IsValidTarget(_q.Range))
            {
                return;
            }

            var prediction = _q.GetPrediction(target);

            if (prediction.Hitchance != HitChance.None && prediction.Hitchance != HitChance.OutOfRange
                && prediction.Hitchance != HitChance.Collision && prediction.Hitchance >= HitChance.Medium)
            {
                _q.Cast(target);
            }
            else if (ParamBool("qSmite") && _q.IsReady() && target.IsValidTarget(_q.Range)
                     && prediction.CollisionObjects.Count(a => a.NetworkId != target.NetworkId && a.IsMinion) == 1
                     && _player.GetSpellSlot(SmiteSpellName()).IsReady())
            {
                _player.Spellbook.CastSpell(
                    _player.GetSpellSlot(SmiteSpellName()),
                    prediction.CollisionObjects.Where(a => a.NetworkId != target.NetworkId && a.IsMinion).ToList()[0
                        ]);

                _q.Cast(prediction.CastPosition);
            }
        }
        private static string SmiteSpellName()
        {
            if (SmiteBlue.Any(a => _player.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }

            if (SmiteRed.Any(a => _player.HasItem(a)))
            {
                return "s5_summonersmiteduel";
            }

            return "summonersmite";
        }
        private static void Harass(AIHeroClient t)
        {
            if (t == null) return;

            var jumpObject = ObjectManager.Get<AIBaseClient>()
                .OrderBy(obj => obj.Distance(firstpos))
                .FirstOrDefault(obj =>
                    obj.IsAlly && !obj.IsMe &&
                    !(obj.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0) &&
                    obj.Distance(t.Position) < 550);

            if (_config.Item("UseEHar").GetValue<MenuBool>())
                CastECombo();
            if (_config.Item("UseQ1Har").GetValue<MenuBool>())
                CastQ1(t);
            if (_config.Item("UseQ2Har").GetValue<MenuBool>() && (t.HasBuff("BlindMonkQOne") || t.HasBuff("blindmonkqonechaos")) && jumpObject != null && WStage == WCastStage.First)
            {
                _q.Cast();
                q2casttime = Environment.TickCount;
            }
            if (_player.Distance(t.Position) < 300 && !_q.IsReady() && q2casttime + 2500 > Environment.TickCount && Environment.TickCount > q2casttime + 500)
                CastW(jumpObject);

            var useItemsH = _config.Item("UseItemsharass").GetValue<MenuBool>();

            if (useItemsH && _tiamat.IsReady() && t.Distance(_player.Position) < _tiamat.Range)
            {
                _tiamat.Cast();
            }
            if (useItemsH && _hydra.IsReady() && t.Distance(_player.Position) < _hydra.Range)
            {
                _hydra.Cast();
            }

        }

        public static void WardJump(Vector3 pos, bool useWard = true, bool checkObjects = true, bool fullRange = false)
        {
            if (WStage != WCastStage.First)
            {
                return;
            }
            pos = fullRange ? _player.Position.ToVector2().Extend(pos.ToVector2(), 600).ToVector3() : pos;
            WardCastPosition = NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall)
                ? _player.GetPath(pos).Last()
                : pos;
            var jumpObject =
                ObjectManager.Get<AIBaseClient>()
                    .OrderBy(obj => obj.Distance(_player.Position))
                    .FirstOrDefault(
                        obj =>
                            obj.IsAlly && !obj.IsMe &&
                            (!(obj.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0) &&
                             Vector3.DistanceSquared(pos, obj.Position) <= 150 * 150));
            if (jumpObject != null && checkObjects && WStage == WCastStage.First)
            {
                CastW(jumpObject);
                return;
            }
            if (!useWard)
            {
                return;
            }

            if (_player.GetWardSlot() == null || _player.GetWardSlot().CountInSlot == 0)
            {
                return;
            }
            if (_w.IsReady() && _w.Name == "BlindMonkWOne")
            {
                placeward(WardCastPosition);
            }
        }
        private static void placeward(Vector3 castpos)
        {
            if (WStage != WCastStage.First || Environment.TickCount < wardtime + 2000)
            {
                return;
            }
            var ward = _player.GetWardSlot();
            _player.Spellbook.CastSpell(ward.SpellSlot, castpos);
            wardtime = Environment.TickCount;



        }

        private static void wjumpflee()
        {
            if (WStage != WCastStage.First)
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            else
            {
                if (_config.Item("wjmax").GetValue<MenuBool>())
                {
                    _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    WardJump(Game.CursorPos, true, true, true);
                }
                else if (_player.Distance(Game.CursorPos) >= 700 || walljump == true)
                {

                    if (Game.CursorPos.Distance(wallcheck) > 50)
                    {
                        walljump = false;
                        checker = false;
                        for (var i = 0; i < 40; i++)
                        {
                            var p = Game.CursorPos.Extend(_player.Position, 10 * i);
                            if (NavMesh.GetCollisionFlags(p).HasFlag(CollisionFlags.Wall))
                            {
                                jumppoint = p;
                                wallcheck = Game.CursorPos;
                                walljump = true;
                                break;


                            }
                        }


                        if (walljump == true)
                        {
                            foreach (
                              var qPosition in
                                GetPossibleJumpPositions(jumppoint)
                                .OrderBy(qPosition => qPosition.Distance(jumppoint)))
                            {
                                if (_player.Position.Distance(qPosition) < _player.Position.Distance(jumppoint))
                                {
                                    movepoint = qPosition;
                                    if (movepoint.Distance(jumppoint) > 600)
                                        wpos = movepoint.Extend(jumppoint, 595);
                                    else
                                        wpos = jumppoint;

                                    break;
                                }
                                if (qPosition == null)
                                    movepoint = jumppoint;
                                checker = true;
                                break;
                            }


                        }
                    }
                    var jumpObj = ObjectManager.Get<AIBaseClient>()
                         .OrderBy(obj => obj.Distance(_player.Position))
                         .FirstOrDefault(obj => obj.IsAlly && !obj.IsMe && obj.Distance(movepoint) <= 700 &&
                             (!(obj.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0) &&
                             obj.Distance(jumppoint) <= 200));



                    if (walljump == false || movepoint.Distance(Game.CursorPos) > _player.Distance(Game.CursorPos) + 150)
                    {
                        movepoint = Game.CursorPos;
                        jumppoint = Game.CursorPos;

                    }
                    if (jumpObj == null && _player.GetWardSlot() != null && _player.GetWardSlot().CountInSlot != 0)
                        placeward(wpos);
                    if (_player.Position.Distance(jumppoint) <= 700 && jumpObj != null)
                    {
                        CastW(jumpObj);
                        walljump = false;
                    }


                    _player.IssueOrder(GameObjectOrder.MoveTo, movepoint);
                }
                else
                    WardJump(jumppoint, true, true, false);

            }

        }

        private static IEnumerable<Vector3> GetPossibleJumpPositions(Vector3 pos)
        {
            var pointList = new List<Vector3>();

            for (var j = 680; j >= 50; j -= 50)
            {
                var offset = (int)(2 * Math.PI * j / 50);

                for (var i = 0; i <= offset; i++)
                {
                    var angle = i * Math.PI * 2 / offset;
                    var point = new Vector3((float)(pos.X + j * Math.Cos(angle)),
                        (float)(pos.Y - j * Math.Sin(angle)),
                        pos.Z);

                    if (!NavMesh.GetCollisionFlags(point).HasFlag(CollisionFlags.Wall) && point.Distance(_player.Position) < pos.Distance(_player.Position) - 400 &&
                        point.Distance(pos.Extend(_player.Position, 600)) <= 250)
                        pointList.Add(point);
                }
            }

            return pointList;
        }
        private static void Insec(AIHeroClient t)
        {
            if (t == null)
            {
                return;
            }
            if (insobj != null && instypecheck == 2)
                insdirec = insobj.Position;
            if (t.Position.Distance(insdirec) + 100 < _player.Position.Distance(insdirec) && _r.IsReady())
            {
                _r.CastOnUnit(t);
                if (LastCast.LastCastPacketSent.Slot == SpellSlot.R)
                {
                    inscount = Environment.TickCount;
                    canmove = 1;
                }
            }
            if (canmove == 1)
            {
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (!_r.IsReady() || ((_player.GetWardSlot() == null || _player.GetWardSlot().CountInSlot == 0 || WStage != WCastStage.First) && _player.Spellbook.CanUseSpell(_flashSlot) == SpellState.Cooldown))
            {
                canmove = 1;
                return;
            }



            insecpos = t.Position.Extend(insdirec, -300);
            if ((_player.Position.Distance(insecpos) > 600 || inscount + 500 > Environment.TickCount) && t != null && t.IsValidTarget() && QStage == QCastStage.First)
            {
                var qpred = _q.GetPrediction(t);
                if (qpred.Hitchance >= HitChance.Medium)
                    _q.Cast(t);
                if (qpred.Hitchance == HitChance.Collision && _config.Item("minins").GetValue<MenuBool>())
                {
                    var enemyqtry = ObjectManager.Get<AIBaseClient>().Where(enemyq => (enemyq.IsValidTarget() || (enemyq.IsMinion && enemyq.IsEnemy)) && enemyq.Distance(insecpos) < 500);
                    foreach (
                        var enemyhit in enemyqtry.OrderBy(enemyhit => enemyhit.Distance(insecpos)))
                    {

                        if (_q.GetPrediction(enemyhit).Hitchance >= HitChance.Medium && enemyhit.Distance(insecpos) < 500 && _player.GetSpellDamage(enemyhit, SpellSlot.Q) < enemyhit.Health)
                            _q.Cast(enemyhit);
                    }
                }
            }
            if (QStage == QCastStage.Second)
            {
                var enemy = ObjectManager.Get<AIBaseClient>().FirstOrDefault(unit => unit.IsEnemy && (unit.HasBuff("BlindMonkQOne") || unit.HasBuff("blindmonkqonechaos")));
                if (enemy.Position.Distance(insecpos) < 550)
                {
                    _q.Cast();
                    canmove = 0;
                }
            }

            if (_player.Position.Distance(insecpos) < 600)
            {
                if ((_player.GetWardSlot() == null || _player.GetWardSlot().CountInSlot == 0 || WStage != WCastStage.First) && _config.Item("fins").GetValue<MenuBool>() &&
                    _player.Spellbook.CanUseSpell(_flashSlot) == SpellState.Ready && _player.Position.Distance(t.Position) < _r.Range && Environment.TickCount > counttime + 3000)
                {
                    _r.CastOnUnit(t);
                    Utility.DelayAction.Add(Game.Ping + 125, () => _player.Spellbook.CastSpell(_flashSlot, insecpos));
                    canmove = 0;
                }
                else
                    WardJump(insecpos, true, true, false);
                counttime = Environment.TickCount;
                canmove = 0;
            }


        }

        static AIHeroClient GetEnemy
        {
            get
            {
                var t = TargetSelector.SelectedTarget;
                if (t.IsValidTarget(1400))
                {
                    return t;
                }
                return TargetSelector.GetTarget(1400);

            }

        }

        private static QCastStage QStage
        {
            get
            {
                if (!_q.IsReady()) return QCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Name == "BlindMonkQOne"
                    ? QCastStage.First
                    : QCastStage.Second);

            }
        }
        private static ECastStage EStage
        {
            get
            {
                if (!_e.IsReady()) return ECastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Name == "BlindMonkEOne"
                    ? ECastStage.First
                    : ECastStage.Second);

            }
        }


        public static int PassiveStacks
        {
            get
            {
                return _player.GetBuff("blindmonkpasive_cosmetic").Count;
            }
        }

        private static WCastStage WStage
        {
            get
            {
                if (!_w.IsReady()) return WCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "blindmonkwtwo"
                    ? WCastStage.Second
                    : WCastStage.First);

            }
        }


        internal enum QCastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum ECastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum WCastStage
        {
            First,
            Second,
            Cooldown
        }
        private static void CastSelfW()
        {
            if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First) return;

            _w.Cast();
            wcasttime = Environment.TickCount;

        }
        private static void CastW(AIBaseClient obj)
        {
            if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First) return;

            _w.CastOnUnit(obj);
            wcasttime = Environment.TickCount;

        }

        private static void CastECombo()
        {
            if (!_e.IsReady()) return;
            if (ObjectManager.Get<AIHeroClient>()
                .Count(
                    hero =>
                        hero.IsValidTarget() &&
                        hero.Distance(ObjectManager.Player.Position) <= _e.Range) > 0)
            {
                CastE1();
            }
            if (EStage == ECastStage.Second && ((!Passive()) || Environment.TickCount > ecasttime + 2700))
                _e.Cast();
        }
        private static void CastE1()
        {
            if (500 >= Environment.TickCount - ecasttime || EStage != ECastStage.First) return;
            _e.Cast();
            ecasttime = Environment.TickCount;
        }

        private static void CastQ1(AIBaseClient target)
        {
            if (QStage != QCastStage.First) return;
            var qpred = _q.GetPrediction(target);
            if (qpred.Hitchance >= HitChance.Medium && qpred.CastPosition.Distance(_player.Position) < 1100)
            {
                _q.Cast(target);
                firstpos = _player.Position;
                qcasttime = Environment.TickCount;
            }
        }

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static string SmitetypeDmg()
        {
            if (SmiteBlue.Any(a => _player.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(a => _player.HasItem(a)))
            {
                return "s5_summonersmiteduel";

            }
            return "summonersmite";
        }
        private static string SmitetypeHp()
        {
            if (SmitePurple.Any(a => _player.HasItem(a)))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }
        private static void UseItemes(AIHeroClient target)
        {
            var iBilge = _config.Item("Bilge").GetValue<MenuBool>();
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth * (_config.Item("BilgeEnemyhp").GetValue<MenuSlider>().Value) / 100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth * (_config.Item("Bilgemyhp").GetValue<MenuSlider>().Value) / 100);
            var iBlade = _config.Item("Blade").GetValue<MenuBool>();
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth * (_config.Item("BladeEnemyhp").GetValue<MenuSlider>().Value) / 100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth * (_config.Item("Blademyhp").GetValue<MenuSlider>().Value) / 100);
            var iOmen = _config.Item("Omen").GetValue<MenuBool>();
            var iOmenenemys = ObjectManager.Get<AIHeroClient>().Count(hero => hero.IsValidTarget(450)) >=
                              _config.Item("Omenenemys").GetValue<MenuSlider>().Value;
            var iTiamat = _config.Item("Tiamat").GetValue<MenuBool>();
            var iHydra = _config.Item("Hydra").GetValue<MenuBool>();
            var ilotis = _config.Item("lotis").GetValue<MenuBool>();
            var iYoumuu = _config.Item("Youmuu").GetValue<MenuBool>();


            if (_player.Distance(target.Position) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);

            }
            if (_player.Distance(target.Position) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);

            }
            if (_player.Distance(target.Position) <= 300 && iTiamat && _tiamat.IsReady())
            {
                _tiamat.Cast();

            }
            if (_player.Distance(target.Position) <= 300 && iHydra && _hydra.IsReady())
            {
                _hydra.Cast();

            }
            if (iOmenenemys && iOmen && _rand.IsReady())
            {
                _rand.Cast();

            }
            if (ilotis)
            {
                foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsAlly || hero.IsMe))
                {
                    if (hero.Health <= (hero.MaxHealth * (_config.Item("lotisminhp").GetValue<MenuSlider>().Value) / 100) &&
                        hero.Distance(_player.Position) <= _lotis.Range && _lotis.IsReady())
                        _lotis.Cast();
                }
            }
            if (_player.Distance(target.Position) <= 350 && iYoumuu && _youmuu.IsReady())
            {
                _youmuu.Cast();

            }
        }

        private static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.Position, _q.Range);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.Position, _e.Range);
            var useItemsl = _config.Item("UseItemslane").GetValue<MenuBool>();
            var useQl = _config.Item("UseQL").GetValue<MenuBool>();
            var useEl = _config.Item("UseEL").GetValue<MenuBool>();
            if (allMinionsQ.Count == 0)
                return;
            if (EStage == ECastStage.Second && ((!Passive()) || Environment.TickCount > ecasttime + 2700))
                _e.Cast();
            if (QStage == QCastStage.Second && (Environment.TickCount > qcasttime + 2700 || !Passive()))
                _q.Cast();

            foreach (var minion in allMinionsQ)
            {
                if (!minion.InAutoAttackRange() && useQl &&
                    minion.Health < _player.GetSpellDamage(minion, SpellSlot.Q) * 0.70)
                    _q.Cast(minion);
                else if (minion.InAutoAttackRange() && useQl &&
                    minion.Health > _player.GetSpellDamage(minion, SpellSlot.Q) * 2)
                    CastQ1(minion);
            }



            if (_e.IsReady() && useEl)
            {
                if (allMinionsE.Count > 2)
                {
                    CastE1();
                }
                else
                    foreach (var minion in allMinionsE)
                        if (!minion.InAutoAttackRange() &&
                            minion.Health < 0.90 * _player.GetSpellDamage(minion, SpellSlot.E))
                            CastE1();
            }
            if (useItemsl && _tiamat.IsReady() && allMinionsE.Count > 2)
            {
                _tiamat.Cast();
            }
            if (useItemsl && _hydra.IsReady() && allMinionsE.Count > 2)
            {
                _hydra.Cast();
            }
        }

        private static void LastHit()
        {
            var allMinionsQ = GameObjects.GetMinions(_player.Position, _q.Range, MinionTypes.All);
            var useQ = _config.Item("UseQLH").GetValue<MenuBool>();
            foreach (var minion in allMinionsQ)
            {
                if (QStage == QCastStage.First && useQ && _player.Distance(minion.Position) < _q.Range &&
                    minion.Health < 0.90 * _player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    CastQ1(minion);
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = GameObjects.GetJungles(_player.Position, _q.Range,
                JungleType.All, JungleOrderTypes.MaxHealth);
            var useItemsJ = _config.Item("UseItemsjungle").GetValue<MenuBool>();
            var useQ = _config.Item("UseQJ").GetValue<MenuBool>();
            var useW = _config.Item("UseWJ").GetValue<MenuBool>();
            var useE = _config.Item("UseEJ").GetValue<MenuBool>();


            if (mobs.Count > 0)
            {
                var mob = mobs[0];

                if (useItemsJ && _tiamat.IsReady() && _player.Distance(mob.Position) < _tiamat.Range)
                {
                    _tiamat.Cast();
                }
                if (useItemsJ && _hydra.IsReady() && _player.Distance(mob.Position) < _hydra.Range)
                {
                    _hydra.Cast();
                }
                if (QStage == QCastStage.Second && (mob.Health < _q.GetDamage(mob) && ((mob.HasBuff("BlindMonkQOne") || mob.HasBuff("blindmonkqonechaos"))) || Environment.TickCount > qcasttime + 2700 || ((!Passive()))))
                    _q.Cast();
                if (WStage == WCastStage.Second && ((!Passive()) || Environment.TickCount > wcasttime + 2700))
                    _w.Cast();
                if (EStage == ECastStage.Second && ((!Passive()) || Environment.TickCount > ecasttime + 2700))
                    _e.Cast();
                if (!Passive() && useQ && _q.IsReady() || mob.Health < _q.GetDamage(mob) * 2)
                    CastQ1(mob);
                else if (!Passive() && _config.Item("PriW").GetValue<MenuBool>() && useW && _w.IsReady())
                    CastSelfW();
                else if (!Passive() && useE && _e.IsReady() && mob.Distance(_player.Position) < _e.Range || mob.Health < _e.GetDamage(mob))
                    CastE1();

            }
        }
        private static void KillSteal()
        {
            var enemyVisible =
                        ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsValidTarget() && _player.Distance(enemy.Position) <= 600).FirstOrDefault();

            {
                if (_player.GetSummonerSpellDamage(enemyVisible, SummonerSpell.Ignite) > enemyVisible.Health && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, enemyVisible);
                }
            }
            if (_r.IsReady() && _config.Item("UseRM").GetValue<MenuBool>())
            {
                var t = TargetSelector.GetTarget(_r.Range);
                if (_player.GetSpellDamage(t, SpellSlot.R) > t.Health && _player.Distance(t.Position) <= _r.Range)
                    _r.CastOnUnit(t);
            }


            if (_e.IsReady() && _config.Item("UseEM").GetValue<MenuBool>())
            {
                var t = TargetSelector.GetTarget(_e.Range);
                if (_e.GetDamage(t) > t.Health && _player.Distance(t.Position) <= _e.Range)
                {
                    _e.Cast();
                }
            }
        }




        private static void Drawing_OnDraw(EventArgs args)

        {
            if (checker)
            {
                Drawing.DrawText(Drawing.WorldToScreen(jumppoint)[0] + 50,
                                Drawing.WorldToScreen(jumppoint)[1] + 40, Color.Red,
                                "NOT JUMPABLE");
            }
            if (_config.Item("wjump").GetValue<MenuKeyBind>().Active)
            {
                Render.Circle.DrawCircle(jumppoint, 50, System.Drawing.Color.Red);
                Render.Circle.DrawCircle(movepoint, 50, System.Drawing.Color.White);
            }

            if (_config.Item("insc").GetValue<MenuKeyBind>().Active)
            {

                Render.Circle.DrawCircle(insecpos, 75, System.Drawing.Color.Blue);
                Render.Circle.DrawCircle(insdirec, 100, System.Drawing.Color.Green);
            }


            if (_config.Item("damagetest").GetValue<MenuBool>())
            {
                foreach (
                    var enemyVisible in
                        ObjectManager.Get<AIHeroClient>().Where(enemyVisible => enemyVisible.IsValidTarget()))

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red,
                            "Combo=Rekt");
                    }

            }


            if (_config.Item("CircleLag").GetValue<MenuBool>())
            {
                if (_config.Item("DrawQ").GetValue<MenuBool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Blue);
                }
                if (_config.Item("DrawE").GetValue<MenuBool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawR").GetValue<MenuBool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.Blue);
                }
            }
            else
            {
                if (_config.Item("DrawQ").GetValue<MenuBool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawE").GetValue<MenuBool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }

                if (_config.Item("DrawR").GetValue<MenuBool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.White);
                }
            }

        }


    }
}
