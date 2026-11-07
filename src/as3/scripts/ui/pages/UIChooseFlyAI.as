package ui.pages
{
   import f4.data.InitData;
   import f4data.F4DataLinks;
   import f4data.F4ShowData;
   import f4data.MC2F4Data;
   import f4data.cpx.F4DataCPX;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFormatAlign;
   import fullshit.ctrl2.SoundUnit2;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.unit.FormChooseFlyAI;
   import ui.unit.ShowPart;
   
   public class UIChooseFlyAI extends UIBase
   {
      
      public var form:FormChooseFlyAI;
      
      public var mc2f4:MC2F4Data;
      
      public var f4datax:F4DataCPX;
      
      public var dtLinkF:F4DataLinks;
      
      public var dtLinkS:F4DataLinks;
      
      public var ais:Array;
      
      public var aiXnum:Number;
      
      public var tempPlayerNum:int = -1;
      
      public var ainum:int = 20;
      
      private static const ROLE_SHORT_NAMES:Array = ["ANC", "ESC", "EVD", "HNT", "SUP", "SHD"];
      
      private static const ROLE_FULL_NAMES:Array = [
         "HOLD / ANCHOR",
         "ESCORT",
         "EVADE",
         "SEEK / HUNT",
         "FIRE SUPPORT",
         "SHIELD"
      ];
      
      private var _slotContainers:Array;
      
      private var _roleLabels:Array;
      
      private var _leaderLabels:Array;
      
      private var _dropdownPopup:Sprite;
      
      private var _activeDropdownSlot:int = -1;
      
      public function UIChooseFlyAI(param1:F4DataCPX, param2:DisplayObjectContainer, param3:MovieClip, param4:String, param5:UICtrl, param6:UIBase = null)
      {
         ConfigAndStory.checkCaptainShip();
         var _capFly:F4DataLinks = ConfigAndStory.getCaptainFly();
         if(_capFly)
         {
            if(ConfigAndStory.PLAYER == _capFly.index)
            {
               ConfigAndStory.PLAYER = 0;
            }
            var _aiSlot:int = 0;
            var _cLen:int = ConfigAndStory.COMPANIONS ? ConfigAndStory.COMPANIONS.length : 0;
            while(_aiSlot < _cLen)
            {
               var _cSlotItem:Object = ConfigAndStory.COMPANIONS[_aiSlot] as Object;
               if(_cSlotItem && _cSlotItem.craftIndex == _capFly.index)
               {
                  ConfigAndStory.COMPANIONS[_aiSlot] = null;
               }
               _aiSlot++;
            }
         }
         param3.getChildByName("lv").visible = false;
         (param3.getChildByName("txtbt") as TextField).text = "BATTLE-" + (ConfigAndStory.STORY + 1);
         this.f4datax = param1;
         this.form = new FormChooseFlyAI(param3.getChildByName("form") as MovieClip,param1.plFly);
         (param3.getChildByName("lock1") as MovieClip).gotoAndStop(1);
         (param3.getChildByName("lock2") as MovieClip).gotoAndStop(1);
         this.form.updateAI();
         this.form.initsortMC();
         (param3.getChildByName("shopnew") as MovieClip).mouseEnabled = false;
         (param3.getChildByName("shopnew") as MovieClip).mouseChildren = false;
         if(!ConfigAndStory.SHOPNEW || ConfigAndStory.STORY == 1 || ConfigAndStory.STORY == 99)
         {
            param3.getChildByName("shopnew").visible = false;
         }
         ConfigAndStory.save();
         super(param2,param3,param4,param5,param6);
         this.mc2f4 = new MC2F4Data();
         this.ais = [];
         this.aiXnum = (_mov.getChildByName("form") as MovieClip).getChildByName("$choosesort3").x;
         this.fillAI();
         this.initCompanionUI();
         var _loc7_:F4DataLinks = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER];
         if(_loc7_)
         {
            this.addDtL(_loc7_,this.dtLinkF,"showFly",false);
         }
         InitData.SOUNDC2.allEnd(true,"M_Menu");
         InitData.SOUNDC2.addSound(new SoundUnit2(InitData.SOUNDDATA.musicArray[1],"M_Menu"),int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
      }
      
      override public function afterFadeIn() : void
      {
         var _loc1_:F4DataLinks = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER];
         if(!_loc1_)
         {
            disableBtn("ready");
         }
         this.form.initsortMC();
         this.form.sort(0);
         this.form.sort(0);
         if(ConfigAndStory.STORY == 10)
         {
            if(ConfigAndStory.SHOWTEXTMENU[0] == 0)
            {
               ConfigAndStory.DIRECTOR.beforePlace(this.afterTalk,this,2,3,0);
               ConfigAndStory.SHOWTEXTMENU[0] = 1;
               ConfigAndStory.saveData();
            }
            else
            {
               this.afterTalk();
            }
         }
         else if(ConfigAndStory.SHOWTEXTMENU[1] == 0)
         {
            ConfigAndStory.DIRECTOR.beforePlace(this.afterTalk,this,2,0,0);
            ConfigAndStory.SHOWTEXTMENU[1] = 1;
            ConfigAndStory.saveData();
         }
         else
         {
            this.afterTalk();
         }
      }
      
      public function afterTalk() : void
      {
         super.afterFadeIn();
         if(ConfigAndStory.STORY >= 14)
         {
            (_mov.getChildByName("lock1") as MovieClip).gotoAndPlay(1);
            (_mov.getChildByName("lock2") as MovieClip).gotoAndPlay(1);
            if(!ConfigAndStory.TIP_COMPANION_UNLOCK)
            {
               ConfigAndStory.TIP_COMPANION_UNLOCK = true;
               ConfigAndStory.saveData();
               new UIReally(null,null,null,"AI Companions Unlocked","\nAI companion deployment is now active!\nAssign ships to your wingmen and set their pre-battle roles (Escort, Hunt, Shield, etc.).\nCommand them in combat using the Tactical HUD.",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
         }
         if(ConfigAndStory.STORY == 1)
         {
            if(ConfigAndStory.HOWTOPLAY[1] == 0)
            {
               new UIHelpX2(1,"Center",_sp,Attach.attach("UI_HelpBuildOneFirst") as MovieClip,uiName,_ui,this);
            }
         }
         else if(ConfigAndStory.STORY == 2)
         {
            if(ConfigAndStory.HOWTOPLAY[8] == 0)
            {
               new UIHelpX2(8,"Center",_sp,Attach.attach("UI_HelpOneSec") as MovieClip,uiName,_ui,this);
            }
         }
         else if(ConfigAndStory.STORY >= 14)
         {
            if(ConfigAndStory.HOWTOPLAY[2] == 0)
            {
               new UIHelpX2(2,"Center",_sp,Attach.attach("UI_HelpChooseCraftAI") as MovieClip,uiName,_ui,this);
            }
         }
      }
      
      override public function removeSelf() : UIBase
      {
         this.closeRoleDropdown();
         super.removeSelf();
         return _last;
      }
      
      override public function outAct(param1:String) : void
      {
         if(param1.indexOf("form") != -1)
         {
            this.form.disableLight();
         }
      }
      
      override public function overAct(param1:String) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataLinks = null;
         var _loc4_:int = 0;
         if(param1.indexOf("form") != -1)
         {
            _loc2_ = parseInt(param1.substr(4));
            _loc3_ = this.form.getLink(_loc2_);
            if(_loc3_)
            {
               this.addDtL(_loc3_,this.dtLinkS,"showDetail",true);
               this.form.lightOn(_loc2_);
            }
         }
         else if(param1.charAt(0) == "a")
         {
            _loc2_ = parseInt(param1.substr(1)) - 1;
            var _compHover:Object = ConfigAndStory.COMPANIONS && _loc2_ < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[_loc2_] as Object : null;
            if(_compHover && _compHover.craftIndex >= 0)
            {
               _loc4_ = _compHover.craftIndex;
               _loc3_ = this.f4datax.plFly[_loc4_];
               if(_loc3_)
               {
                  this.addDtL(_loc3_,this.dtLinkS,"showDetail",true);
               }
            }
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var sdata:F4ShowData = null;
         var sub:int = 0;
         var formNum:int = 0;
         var f4link:F4DataLinks = null;
         var plNum:int = 0;
         var parentStrAI:String = null;
         var formNumAI:int = 0;
         var f4linkAI:F4DataLinks = null;
         var aiNum:int = 0;
         var btn:BtnBase = param1;
         var btnStr:String = btn.name;
         if(btnStr == "back")
         {
            allRemoved = function():void
            {
               new UIMain(_sp,Attach.attach("UI_Main") as MovieClip,"UI_Main",_ui);
            };
            ConfigAndStory.saveData();
            this.removeSelf();
         }
         else if(btnStr == "help")
         {
            if(ConfigAndStory.STORY < 14)
            {
               new UIHelpX2(1,"Center",_sp,Attach.attach("UI_HelpChooseCraft") as MovieClip,uiName,_ui,this);
            }
            else
            {
               new UIHelpX2(2,"Center",_sp,Attach.attach("UI_HelpChooseCraftAI") as MovieClip,uiName,_ui,this);
            }
         }
         else if(btnStr == "factory")
         {
            allRemoved = function():void
            {
               new UIMakePlayer(ConfigAndStory.ALLPART,_sp,Attach.attach("UI_MakePlayer") as MovieClip,"UI_MakePlayer",_ui);
            };
            this.removeSelf();
         }
         else if(btnStr == "shop")
         {
            ConfigAndStory.SHOPNEW = false;
            allRemoved = function():void
            {
               new UIShop(ConfigAndStory.ALLPART,_sp,Attach.attach("UI_Shop") as MovieClip,"UI_Shop",_ui);
            };
            this.removeSelf();
         }
         else if(btnStr == "next")
         {
            this.form.next(true);
         }
         else if(btnStr == "prev")
         {
            this.form.next(false);
         }
         else if(btnStr == "first")
         {
            this.form.toPage(1);
         }
         else if(btnStr == "last")
         {
            this.form.toPage(this.form.allPage);
         }
         else if(btnStr == "mission")
         {
            new UIMission(_sp,Attach.attach("UI_Mission") as MovieClip,"UI_Mission",_ui,this);
         }
         else if(btnStr == "ready")
         {
            new UIReally(this.readyYes,null,null,"Ready?","Ready to fight?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
         else if(btnStr.indexOf("choosesort") != -1)
         {
            sub = parseInt(btnStr.substr(10));
            this.form.sort(sub);
         }
         else if(btnStr.indexOf("form") != -1)
         {
            if(InitData.STAGE.mouseX < 520)
            {
               formNum = parseInt(btnStr.substr(4));
               f4link = this.form.getLink(formNum);
               if(f4link)
               {
                  if(f4link.isCaptainShip())
                  {
                     this.addDtL(f4link,this.dtLinkS,"showDetail",true);
                     this.form.lightOn(formNum);
                     InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                     return;
                  }
                  this.tempPlayerNum = formNum;
                  plNum = int(this.form.workArr[this.tempPlayerNum + this.form.page * this.form.pageLen][0]);
                  if(this.ckAIUseAI(plNum) == -1)
                  {
                     this.form.updatePlayer(this.tempPlayerNum);
                     sdata = new F4ShowData(f4link);
                     sdata.makeFlyClone();
                     this.addDtL(f4link,this.dtLinkF,"showFly",false);
                     InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                     this.addDtL(f4link,this.dtLinkS,"showDetail",true);
                     this.form.lightOn(formNum);
                  }
                  else
                  {
                     new UIReally(this.changeAI2PlayerYes,null,null,"Use AI\'s?","Really wanna pick this one?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
                  }
               }
            }
         }
         else if(btnStr == "bai")
         {
            parentStrAI = btn.parentName;
            formNumAI = parseInt(parentStrAI.substr(5));
            f4linkAI = this.form.getLink(formNumAI);
            if(f4linkAI)
            {
               if(f4linkAI.isCaptainShip())
               {
                  return;
               }
               this.addAI(f4linkAI.index);
               this.form.updateAI();
               this.addDtL(f4linkAI,this.dtLinkS,"showDetail",true);
            }
         }
         else if(btnStr.charAt(0) == "a")
         {
            aiNum = parseInt(btnStr.substr(1)) - 1;
            var _compAct:Object = ConfigAndStory.COMPANIONS && aiNum < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[aiNum] as Object : null;
            if(_compAct == null || _compAct.craftIndex < 0)
            {
               this.reAI(aiNum);
            }
            else
            {
               this.delAI(aiNum);
            }
            this.form.updateAI();
         }
         else if(btnStr == "reset")
         {
            this.resetAI();
            this.form.updateAI();
         }
         else if(btnStr == "best")
         {
            this.fillBestAI();
            this.form.updateAI();
         }
      }
      
      public function changeAI2PlayerYes() : void
      {
         var _loc2_:F4ShowData = null;
         var _loc1_:F4DataLinks = this.form.getLink(this.tempPlayerNum);
         if(_loc1_)
         {
            this.form.updatePlayer(this.tempPlayerNum);
            _loc2_ = new F4ShowData(_loc1_);
            _loc2_.makeFlyClone();
            this.addDtL(_loc1_,this.dtLinkF,"showFly",false);
            this.findAIUse();
            this.fillAI();
            this.updateCompanionUI();
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            this.form.updateAI();
            this.addDtL(_loc1_,this.dtLinkS,"showDetail",true);
            this.form.lightOn(this.tempPlayerNum);
         }
         this.tempPlayerNum = -1;
      }
      
      public function readyYes() : void
      {
         var tmpui:UIBase = null;
         var aiNums:int = 0;
         var i:int = 0;
         ConfigAndStory.saveData();
         ConfigAndStory.save();
         tmpui = this;
         allRemoved = function():void
         {
            ConfigAndStory.DIRECTOR.beforeLevelMov(playGame,tmpui);
         };
         InitData.CORE.repair.flys.length = 0;
         InitData.CORE.levelData.afa.length = 0;
         if(ConfigAndStory.noNext())
         {
            InitData.CORE.repair.flys.push(this.f4datax.plFly[ConfigAndStory.PLAYER]);
         }
         var _capFly:F4DataLinks = ConfigAndStory.getCaptainFly();
         if(_capFly)
         {
            InitData.CORE.repair.flys.push(_capFly);
         }
         if(ConfigAndStory.STORY != 49)
         {
            i = 0;
            var _compReadyLen:int = ConfigAndStory.COMPANIONS ? ConfigAndStory.COMPANIONS.length : 0;
            while(i < _compReadyLen)
            {
               var _cReady:Object = ConfigAndStory.COMPANIONS[i] as Object;
               if(_cReady != null && _cReady.craftIndex >= 0)
               {
                  aiNums++;
                  InitData.CORE.levelData.afa.push(this.f4datax.plFly[_cReady.craftIndex]);
                  InitData.CORE.repair.flys.push(this.f4datax.plFly[_cReady.craftIndex]);
               }
               i++;
            }
         }
         if(aiNums > 0)
         {
            MochiEvents.trackEvent("ready_ais",aiNums);
         }
         this.removeSelf();
      }
      
      public function playGame() : void
      {
         new UIMapBuild(_sp,Attach.attach("UI_MapBuilding") as MovieClip,"UI_MapBuilding",_ui);
      }
      
      public function ckPlayer() : F4DataLinks
      {
         return this.f4datax.plFly[ConfigAndStory.PLAYER];
      }
      
      public function addDtL(param1:F4DataLinks, param2:F4DataLinks, param3:String, param4:Boolean) : void
      {
         var _loc5_:MovieClip = null;
         var _loc6_:F4ShowData = null;
         if(param2 != param1)
         {
            _loc5_ = _mov.getChildByName(param3) as MovieClip;
            _loc6_ = new F4ShowData(param1,InitData.SELL);
            _loc6_.initData();
            _loc6_.makeFlyClone();
            ShowPart.addFly(_loc6_,_loc5_,param4);
            param2 = param1;
         }
      }
      
      public function cleanDtL(param1:String, param2:F4DataLinks, param3:Boolean) : void
      {
         ShowPart.cleanFly(_mov.getChildByName(param1) as MovieClip,param3);
         param2 = null;
      }
      
      public function ckAIUseAI(param1:int) : int
      {
         if(!ConfigAndStory.COMPANIONS)
         {
            return -1;
         }
         var _loc3_:int = 0;
         while(_loc3_ < this.ainum)
         {
            var _c:Object = _loc3_ < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[_loc3_] as Object : null;
            if(_c != null && _c.craftIndex == param1)
            {
               return _loc3_;
            }
            _loc3_++;
         }
         return -1;
      }
      
      public function ckAIUse(param1:int) : Boolean
      {
         if(this.form && this.form.isCaptainShip(param1))
         {
            return true;
         }
         if(ConfigAndStory.PLAYER == param1)
         {
            return true;
         }
         if(!ConfigAndStory.COMPANIONS)
         {
            return false;
         }
         var _loc3_:int = 0;
         while(_loc3_ < this.ainum)
         {
            var _c:Object = _loc3_ < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[_loc3_] as Object : null;
            if(_c != null && _c.craftIndex == param1)
            {
               return true;
            }
            _loc3_++;
         }
         return false;
      }
      
      public function addAI(param1:int) : void
      {
         var _loc2_:F4DataLinks = null;
         if(this.f4datax && this.f4datax.plFly && param1 >= 0 && param1 < this.f4datax.plFly.length)
         {
            var _capCheck:F4DataLinks = this.f4datax.plFly[param1];
            if(_capCheck && _capCheck.isCaptainShip())
            {
               return;
            }
         }
         var _loc3_:F4ShowData = null;
         var _loc4_:Array = null;
         var _loc5_:int = 0;
         var _loc6_:Boolean = false;
         if(!this.ckAIUse(param1))
         {
            _loc2_ = this.f4datax.plFly[param1];
            _loc3_ = new F4ShowData(_loc2_);
            _loc3_.initData();
            _loc6_ = false;
            _loc5_ = 0;
            while(_loc5_ < this.ainum)
            {
               var _cAdd:Object = ConfigAndStory.COMPANIONS && _loc5_ < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[_loc5_] as Object : null;
               if(_cAdd == null || _cAdd.craftIndex < 0)
               {
                  var _existingComp:Object = this.getOrCreateCompanion(_loc5_);
                  _existingComp.craftIndex = param1;
                  var _aipad1:MovieClip = _mov.getChildByName("aipad") as MovieClip;
                  var _txtFld1:TextField = _aipad1 ? _aipad1.getChildByName("$a" + (_loc5_ + 1)) as TextField : null;
                  if(_txtFld1)
                  {
                     _txtFld1.text = _loc3_.cls;
                  }
                  this.ais[_loc5_] = param1;
                  this.updateCompanionUI();
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                  _loc6_ = true;
                  break;
               }
               _loc5_++;
            }
            if(!_loc6_)
            {
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
            }
         }
      }
      
      public function reAI(param1:int) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataLinks = null;
         var _loc4_:F4ShowData = null;
         var _loc5_:Array = null;
         if(this.ais[param1] != null)
         {
            _loc2_ = int(this.ais[param1]);
            if(!this.ckAIUse(_loc2_))
            {
               _loc3_ = this.f4datax.plFly[_loc2_];
               _loc4_ = new F4ShowData(_loc3_);
               _loc4_.initData();
               var _existingReComp:Object = this.getOrCreateCompanion(param1);
               _existingReComp.craftIndex = _loc2_;
               var _aipad2:MovieClip = _mov.getChildByName("aipad") as MovieClip;
               var _txtFld2:TextField = _aipad2 ? _aipad2.getChildByName("$a" + (param1 + 1)) as TextField : null;
               if(_txtFld2)
               {
                  _txtFld2.text = _loc4_.cls;
               }
               this.updateCompanionUI();
               this.addDtL(_loc3_,this.dtLinkS,"showDetail",true);
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            }
         }
      }
      
      public function delAI(param1:int) : void
      {
         var _loc2_:F4DataLinks = null;
         if(ConfigAndStory.COMPANIONS && param1 < ConfigAndStory.COMPANIONS.length)
         {
            ConfigAndStory.COMPANIONS[param1] = null;
         }
         this.closeRoleDropdown();
         this.updateCompanionUI();
         var _aipad3:MovieClip = _mov.getChildByName("aipad") as MovieClip;
         var _txtFld3:TextField = _aipad3 ? _aipad3.getChildByName("$a" + (param1 + 1)) as TextField : null;
         if(_txtFld3)
         {
            _txtFld3.text = "-";
         }
         if(this.ais[param1] != null)
         {
            _loc2_ = this.f4datax.plFly[this.ais[param1]];
            this.addDtL(_loc2_,this.dtLinkS,"showDetail",true);
         }
      }
      
      public function findAIUse() : void
      {
         var _loc2_:int = 0;
         _loc2_ = 0;
         while(_loc2_ < this.ainum)
         {
            var _cUse:Object = ConfigAndStory.COMPANIONS && _loc2_ < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[_loc2_] as Object : null;
            if(_cUse != null && _cUse.craftIndex == ConfigAndStory.PLAYER)
            {
               ConfigAndStory.COMPANIONS[_loc2_] = null;
            }
            _loc2_++;
         }
      }
      
      public function fillAI() : void
      {
         var _loc2_:F4DataLinks = null;
         var _loc4_:int = 0;
         var _loc3_:F4ShowData = new F4ShowData(_loc2_);
         var _aipad:MovieClip = _mov.getChildByName("aipad") as MovieClip;
         _loc4_ = 0;
         while(_loc4_ < this.ainum)
         {
            var _txtFld:TextField = _aipad ? _aipad.getChildByName("$a" + (_loc4_ + 1)) as TextField : null;
            var _cFill:Object = ConfigAndStory.COMPANIONS && _loc4_ < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[_loc4_] as Object : null;
            if(_cFill != null && _cFill.craftIndex >= 0 && this.f4datax != null && this.f4datax.plFly != null)
            {
               _loc2_ = this.f4datax.plFly[_cFill.craftIndex];
               if(_loc2_ != null)
               {
                  _loc3_.link = _loc2_;
                  _loc3_.initData();
                  if(_txtFld != null)
                  {
                     _txtFld.text = _loc3_.cls;
                  }
                  this.ais[_loc4_] = _cFill.craftIndex;
               }
               else
               {
                  if(_txtFld != null)
                  {
                     _txtFld.text = "-";
                  }
               }
            }
            else
            {
               if(_txtFld != null)
               {
                  _txtFld.text = "-";
               }
            }
            _loc4_++;
         }
         this.updateCompanionUI();
      }
      
      public function resetAI() : void
      {
         var _loc1_:int = 0;
         _loc1_ = 0;
         while(_loc1_ < this.ainum)
         {
            this.delAI(_loc1_);
            _loc1_++;
         }
      }
      
      public function findBestAI() : int
      {
         var _loc2_:F4DataLinks = null;
         var _loc1_:F4ShowData = new F4ShowData(null);
         var _loc3_:int = 0;
         var _loc4_:int = -1;
         for each(_loc2_ in this.f4datax.plFly)
         {
            if(!this.ckAIUse(_loc2_.index))
            {
               _loc1_.link = _loc2_;
               _loc1_.initData();
               if(_loc1_.value > _loc3_)
               {
                  _loc4_ = _loc1_.link.index;
                  _loc3_ = _loc1_.value;
               }
            }
         }
         return _loc4_;
      }
      
      public function fillBestAI() : void
      {
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         _loc2_ = 0;
         while(_loc2_ < 5)
         {
            var _cBest:Object = ConfigAndStory.COMPANIONS && _loc2_ < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[_loc2_] as Object : null;
            if(_cBest == null || _cBest.craftIndex < 0)
            {
               _loc3_ = this.findBestAI();
               if(_loc3_ >= 0)
               {
                  this.addAI(_loc3_);
               }
            }
            _loc2_++;
         }
      }

      private function onMovClicked(param1:MouseEvent) : void
      {
         if(!this._dropdownPopup)
         {
            return;
         }
         var _target:DisplayObject = param1.target as DisplayObject;
         if(!_target)
         {
            return;
         }
         if(this._dropdownPopup.contains(_target))
         {
            return;
         }
         var _i:int = 0;
         while(_i < 5)
         {
            var _cCont:Sprite = this._slotContainers ? this._slotContainers[_i] as Sprite : null;
            if(_cCont && _cCont.contains(_target))
            {
               return;
            }
            _i++;
         }
         this.closeRoleDropdown();
      }

      private function getOrCreateCompanion(param1:int) : Object
      {
         if(!ConfigAndStory.COMPANIONS)
         {
            ConfigAndStory.COMPANIONS = [];
         }
         var _comp:Object = param1 < ConfigAndStory.COMPANIONS.length ? ConfigAndStory.COMPANIONS[param1] as Object : null;
         if(!_comp)
         {
            _comp = ConfigAndStory.makeCompanionAssignment(-1, ConfigAndStory.ROLE_ESCORT, ConfigAndStory.LEADER_MICHAEL);
            ConfigAndStory.COMPANIONS[param1] = _comp;
         }
         return _comp;
      }

      private function initCompanionUI() : void
      {
         var _aipad:MovieClip = _mov.getChildByName("aipad") as MovieClip;
         if(!_aipad)
         {
            return;
         }
         this._slotContainers = [];
         this._roleLabels = [];
         this._leaderLabels = [];
         
         var _slotX:Array = [-24, 2, 28, 54, 80];
         var _i:int = 0;
         while(_i < 5)
         {
            var _cCont:Sprite = new Sprite();
            _cCont.name = "compSlot_" + _i;
            _cCont.x = _slotX[_i];
            _cCont.y = 26;
            
            // Role Button
            var _btnRole:Sprite = new Sprite();
            _btnRole.name = "btnRole_" + _i;
            _btnRole.buttonMode = true;
            _btnRole.graphics.beginFill(0x081524, 0.95);
            _btnRole.graphics.lineStyle(1, 0x0099cc, 0.8);
            _btnRole.graphics.drawRoundRect(0, 0, 22, 11, 2, 2);
            _btnRole.graphics.endFill();
            
            var _txtR:TextField = new TextField();
            _txtR.x = 0;
            _txtR.y = -1;
            _txtR.width = 22;
            _txtR.height = 11;
            _txtR.selectable = false;
            _txtR.mouseEnabled = false;
            UIBase.applyFormat(_txtR, UIBase.FONT_BODY, 8, UIBase.COLOR_ACCENT_CYAN, true, TextFormatAlign.CENTER);
            _txtR.text = "ESC";
            _btnRole.addChild(_txtR);
            _btnRole.addEventListener(MouseEvent.CLICK, this.createRoleBtnClickHandler(_i));
            _cCont.addChild(_btnRole);
            this._roleLabels[_i] = _txtR;
            
            // Leader Toggle Button
            var _btnLead:Sprite = new Sprite();
            _btnLead.name = "btnLead_" + _i;
            _btnLead.y = 13;
            _btnLead.buttonMode = true;
            _btnLead.graphics.beginFill(0x0a1c14, 0.95);
            _btnLead.graphics.lineStyle(1, 0x33cc66, 0.8);
            _btnLead.graphics.drawRoundRect(0, 0, 22, 11, 2, 2);
            _btnLead.graphics.endFill();
            
            var _txtL:TextField = new TextField();
            _txtL.x = 0;
            _txtL.y = -1;
            _txtL.width = 22;
            _txtL.height = 11;
            _txtL.selectable = false;
            _txtL.mouseEnabled = false;
            UIBase.applyFormat(_txtL, UIBase.FONT_BODY, 8, UIBase.COLOR_SUCCESS, true, TextFormatAlign.CENTER);
            _txtL.text = "[M]";
            _btnLead.addChild(_txtL);
            _btnLead.addEventListener(MouseEvent.CLICK, this.createLeaderClickHandler(_i));
            _cCont.addChild(_btnLead);
            this._leaderLabels[_i] = _txtL;
            
            _aipad.addChild(_cCont);
            this._slotContainers[_i] = _cCont;
            _i++;
         }
         _mov.addEventListener(MouseEvent.CLICK, this.onMovClicked);
         this.updateCompanionUI();
      }

      private function updateCompanionUI() : void
      {
         if(!this._slotContainers)
         {
            return;
         }
         var _hasCaptain:Boolean = ConfigAndStory.getCaptainFly() != null;
         var _i:int = 0;
         while(_i < 5)
         {
            var _cCont:Sprite = this._slotContainers[_i] as Sprite;
            if(_cCont)
            {
               var _hasCraft:Boolean = ConfigAndStory.COMPANIONS && _i < ConfigAndStory.COMPANIONS.length && ConfigAndStory.COMPANIONS[_i] != null && ConfigAndStory.COMPANIONS[_i].craftIndex >= 0;
               if(!_hasCraft)
               {
                  _cCont.visible = false;
               }
               else
               {
                  _cCont.visible = true;
                  var _comp:Object = this.getOrCreateCompanion(_i);
                  var _roleIdx:int = _comp.role >= 0 && _comp.role <= 5 ? _comp.role : ConfigAndStory.ROLE_ESCORT;
                  var _rTxt:TextField = this._roleLabels[_i] as TextField;
                  if(_rTxt)
                  {
                     _rTxt.text = ROLE_SHORT_NAMES[_roleIdx];
                  }
                  var _lTxt:TextField = this._leaderLabels[_i] as TextField;
                  if(_lTxt)
                  {
                     if(_comp.leader == ConfigAndStory.LEADER_CAPTAIN && _hasCaptain)
                     {
                        _lTxt.text = "[C]";
                        _lTxt.textColor = 0xffcc00;
                     }
                     else
                     {
                        _comp.leader = ConfigAndStory.LEADER_MICHAEL;
                        _lTxt.text = "[M]";
                        _lTxt.textColor = 0x33ccff;
                     }
                  }
               }
            }
            _i++;
         }
      }

      private function createRoleBtnClickHandler(param1:int) : Function
      {
         var slot:int = param1;
         return function(param2:MouseEvent):void
         {
            param2.stopPropagation();
            if(_activeDropdownSlot == slot)
            {
               closeRoleDropdown();
            }
            else
            {
               var _btnSp:Sprite = param2.currentTarget as Sprite;
               showRoleDropdown(slot, _btnSp.parent.x - 25, _btnSp.parent.y);
            }
         };
      }

      private function showRoleDropdown(param1:int, param2:Number, param3:Number) : void
      {
         this.closeRoleDropdown();
         this._activeDropdownSlot = param1;
         var _aipad:MovieClip = _mov.getChildByName("aipad") as MovieClip;
         if(!_aipad)
         {
            return;
         }
         this._dropdownPopup = new Sprite();
         this._dropdownPopup.x = param2 < -25 ? -25 : param2;
         this._dropdownPopup.y = param3 + 14;
         
         this._dropdownPopup.graphics.beginFill(0x06111f, 0.98);
         this._dropdownPopup.graphics.lineStyle(1, 0x00ccff, 0.9);
         this._dropdownPopup.graphics.drawRoundRect(0, 0, 80, 6 * 14 + 4, 4, 4);
         this._dropdownPopup.graphics.endFill();
         
         var _k:int = 0;
         while(_k < 6)
         {
            var _item:Sprite = new Sprite();
            _item.name = "roleOpt_" + _k;
            _item.y = 2 + _k * 14;
            _item.buttonMode = true;
            _item.mouseChildren = false;
            
            _item.graphics.beginFill(0x000000, 0.01);
            _item.graphics.drawRect(2, 0, 76, 13);
            _item.graphics.endFill();
            
            var _optTxt:TextField = new TextField();
            _optTxt.x = 4;
            _optTxt.y = 0;
            _optTxt.width = 72;
            _optTxt.height = 13;
            _optTxt.selectable = false;
            _optTxt.mouseEnabled = false;
            UIBase.applyFormat(_optTxt, UIBase.FONT_BODY, 8, UIBase.COLOR_SYSTEM_LIGHT, false, TextFormatAlign.LEFT);
            _optTxt.text = ROLE_SHORT_NAMES[_k] + " " + ROLE_FULL_NAMES[_k];
            _item.addChild(_optTxt);
            
            _item.addEventListener(MouseEvent.MOUSE_OVER, function(param2:MouseEvent):void
            {
               var _sp:Sprite = param2.currentTarget as Sprite;
               _sp.graphics.clear();
               _sp.graphics.beginFill(0x004466, 0.9);
               _sp.graphics.drawRect(2, 0, 76, 13);
               _sp.graphics.endFill();
            });
            _item.addEventListener(MouseEvent.MOUSE_OUT, function(param2:MouseEvent):void
            {
               var _sp:Sprite = param2.currentTarget as Sprite;
               _sp.graphics.clear();
               _sp.graphics.beginFill(0x000000, 0.01);
               _sp.graphics.drawRect(2, 0, 76, 13);
               _sp.graphics.endFill();
            });
            _item.addEventListener(MouseEvent.CLICK, this.createRoleSelectHandler(param1, _k));
            
            this._dropdownPopup.addChild(_item);
            _k++;
         }
         
         _aipad.addChild(this._dropdownPopup);
      }

      private function createRoleSelectHandler(param1:int, param2:int) : Function
      {
         var slot:int = param1;
         var rIdx:int = param2;
         return function(param3:MouseEvent):void
         {
            param3.stopPropagation();
            var _comp:Object = getOrCreateCompanion(slot);
            _comp.role = rIdx;
            ConfigAndStory.saveData();
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            closeRoleDropdown();
            updateCompanionUI();
         };
      }

      private function closeRoleDropdown() : void
      {
         if(this._dropdownPopup)
         {
            if(this._dropdownPopup.parent)
            {
               this._dropdownPopup.parent.removeChild(this._dropdownPopup);
            }
            this._dropdownPopup = null;
         }
         this._activeDropdownSlot = -1;
      }

      private function createLeaderClickHandler(param1:int) : Function
      {
         var slot:int = param1;
         return function(param2:MouseEvent):void
         {
            param2.stopPropagation();
            closeRoleDropdown();
            var _comp:Object = getOrCreateCompanion(slot);
            var _capFly:F4DataLinks = ConfigAndStory.getCaptainFly();
            if(!_capFly)
            {
               _comp.leader = ConfigAndStory.LEADER_MICHAEL;
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
            }
            else
            {
               if(_comp.leader == ConfigAndStory.LEADER_MICHAEL)
               {
                  _comp.leader = ConfigAndStory.LEADER_CAPTAIN;
               }
               else
               {
                  _comp.leader = ConfigAndStory.LEADER_MICHAEL;
               }
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            }
            ConfigAndStory.saveData();
            updateCompanionUI();
         };
      }

   }
}
