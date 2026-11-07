package ui.pages
{
   import f4.data.InitData;
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import f4data.F4DataOrg;
   import f4data.F4ShowData;
   import f4data.MC2F4Data;
   import f4data.cpx.F4DataCPX;
   import f4data.sdata.ConfigAndStory;
   import f4ui.f4partmc.F4PartMC;
   import f4ui.f4partmc.OrgEvent;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.GradientType;
   import flash.display.Graphics;
   import flash.display.MovieClip;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.filters.GlowFilter;
   import flash.geom.Matrix;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   import fullshit.tool.Attach;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.base.UIStyles;
   import ui.make.MakeUI;
   import ui.unit.FormFly;
   import ui.unit.FormPlayerPart;
   import ui.unit.ShowPart;
   
   public class UIMakePlayer extends UIBase
   {
      
      public var form:FormPlayerPart;
      
      public var formFly:FormFly;
      
      public var ui_wing:Sprite;
      
      public var ui_body:Sprite;
      
      public var ui_other:Sprite;
      
      public var uix:Sprite = new Sprite();
      
      public var mc2f4:MC2F4Data;
      
      public var f4datax:F4DataCPX;
      
      public var addFlyReal:UIReally;
      
      public var mergeFlyReal:UIReally;
      
      public var dtOrg:F4DataOrg;
      
      public var dtLink:F4DataLinks;
      
      public var movingPart:F4PartMC;
      
      public var undoList:Array;
      
      public var undoNum:int = 12;
      
      public var nowedit:Boolean;
      
      public var zoomNum:Number = 1;
      
      public var tempZoom:Number = 1;
      
      public var txtWingHint:TextField;
      
      public var txtWings:TextField;
      
      public var btnClearWeapons:Sprite;
      
      public var btnClearWings:Sprite;
      
      public var btnClearEngines:Sprite;
      
      public function UIMakePlayer(param1:F4DataCPX, param2:DisplayObjectContainer, param3:MovieClip, param4:String, param5:UICtrl, param6:UIBase = null)
      {
         ConfigAndStory.checkCaptainShip();
         this.f4datax = param1;
         this.form = new FormPlayerPart(param3.getChildByName("formPart") as MovieClip,param1.plPart);
         this.formFly = new FormFly(param3.getChildByName("formFly") as MovieClip,param1.plFly);
         this.formFly.form.visible = false;
         param3.getChildByName("tmask").visible = false;
         _mov = param3;
         this.showFlys(true);
         this.showFlysX(false);
         this.showFlyEdit(false);
         this.form.initsortMC();
         this.formFly.initsortMC();
         super(param2,param3,param4,param5,param6);
         this.mc2f4 = new MC2F4Data();
         this.ui_wing = new Sprite();
         this.ui_body = new Sprite();
         this.ui_other = new Sprite();
         this.uix.addChild(this.ui_body);
         this.uix.addChild(this.ui_wing);
         this.uix.addChild(this.ui_other);
         var _loc7_:MovieClip = (_mov.getChildByName("showFly") as MovieClip).getChildByName("pic") as MovieClip;
         this.uix.x = _loc7_.x;
         this.uix.y = _loc7_.y;
         _loc7_.parent.addChild(this.uix);
         this.uix.mask = _loc7_;
         this.undoList = [];
         this.undoShow();
         this.initWingHint();
         this.initWingsInDtTemp();
         this.initClearButtons();
      }
      
      public function initClearButtons() : void
      {
         var formMc:MovieClip = this.form.form;
         if(!formMc)
         {
            return;
         }
         this.btnClearWeapons = this.createCyberBtn("CLR GUNS", 282, -24, this.onClearWeaponsClick);
         this.btnClearWings = this.createCyberBtn("CLR WINGS", 338, -24, this.onClearWingsClick);
         this.btnClearEngines = this.createCyberBtn("CLR ENGS", 394, -24, this.onClearEnginesClick);
         
         formMc.addChild(this.btnClearWeapons);
         formMc.addChild(this.btnClearWings);
         formMc.addChild(this.btnClearEngines);
         
         this.updateClearButtons();
      }
      
      private function createCyberBtn(label:String, xPos:Number, yPos:Number, onClick:Function) : Sprite
      {
         var btn:Sprite = new Sprite();
         btn.x = xPos;
         btn.y = yPos;
         btn.buttonMode = true;
         btn.useHandCursor = true;
         btn.mouseChildren = false;

         var w:Number = 52;
         var h:Number = 19;

         var drawBtn:Function = function(hover:Boolean, down:Boolean):void
         {
            btn.graphics.clear();
            var bgColor:uint = down ? 0x0a1824 : (hover ? 0x1a3854 : 0x0f2233);
            var borderColor:uint = hover ? 0x66ccff : 0x2d5a7b;
            var borderAlpha:Number = hover ? 1.0 : 0.7;
            btn.graphics.lineStyle(1, borderColor, borderAlpha, true);
            btn.graphics.beginFill(bgColor, 0.9);
            btn.graphics.drawRoundRect(0, 0, w, h, 4, 4);
            btn.graphics.endFill();
         };

         drawBtn(false, false);

         var txt:TextField = new TextField();
         var tf:TextFormat = new TextFormat("Arial", 9, 0x88ddff, true);
         tf.align = TextFormatAlign.CENTER;
         txt.defaultTextFormat = tf;
         txt.text = label;
         txt.width = w;
         txt.height = h;
         txt.y = 2;
         txt.selectable = false;
         txt.mouseEnabled = false;
         txt.filters = [new GlowFilter(0x003366, 0.8, 3, 3, 2, 1)];
         btn.addChild(txt);

         btn.addEventListener(MouseEvent.MOUSE_OVER, function(e:MouseEvent):void
         {
            drawBtn(true, false);
            txt.textColor = 0xffffff;
            btn.filters = [new GlowFilter(0x00ccff, 0.6, 4, 4, 2, 1)];
         });

         btn.addEventListener(MouseEvent.MOUSE_OUT, function(e:MouseEvent):void
         {
            drawBtn(false, false);
            txt.textColor = 0x88ddff;
            btn.filters = [];
         });

         btn.addEventListener(MouseEvent.MOUSE_DOWN, function(e:MouseEvent):void
         {
            drawBtn(true, true);
         });

         btn.addEventListener(MouseEvent.MOUSE_UP, function(e:MouseEvent):void
         {
            drawBtn(true, false);
         });

         btn.addEventListener(MouseEvent.CLICK, onClick);

         return btn;
      }
      
      public function updateClearButtons() : void
      {
         if(this.btnClearWeapons)
         {
            var wCount:int = this.countPartsByType(F4DataOrg.TYPE_GUN);
            this.btnClearWeapons.alpha = wCount > 0 ? 1.0 : 0.35;
            this.btnClearWeapons.mouseEnabled = wCount > 0;
         }
         if(this.btnClearWings)
         {
            var wingCount:int = this.countPartsByType(F4DataOrg.TYPE_WING);
            this.btnClearWings.alpha = wingCount > 0 ? 1.0 : 0.35;
            this.btnClearWings.mouseEnabled = wingCount > 0;
         }
         if(this.btnClearEngines)
         {
            var engCount:int = this.countPartsByType(F4DataOrg.TYPE_ENGINE);
            this.btnClearEngines.alpha = engCount > 0 ? 1.0 : 0.35;
            this.btnClearEngines.mouseEnabled = engCount > 0;
         }
      }
      
      public function onClearWeaponsClick(param1:MouseEvent) : void
      {
         var count:int = this.countPartsByType(F4DataOrg.TYPE_GUN);
         if(count == 0)
         {
            return;
         }
         new UIReally(this.cleanWeaponsYes, null, null, "Clear Weapons?", "\nStrip all " + count + " weapons to inventory?", _sp, Attach.attach("UI_Really") as MovieClip, "UI_Really", _ui, this);
      }
      
      public function cleanWeaponsYes() : void
      {
         this.cleanWeapons();
      }
      
      public function onClearWingsClick(param1:MouseEvent) : void
      {
         var count:int = this.countPartsByType(F4DataOrg.TYPE_WING);
         if(count == 0)
         {
            return;
         }
         new UIReally(this.cleanWingsYes, null, null, "Clear Wings?", "\nStrip all wings and attached gear to inventory?", _sp, Attach.attach("UI_Really") as MovieClip, "UI_Really", _ui, this);
      }
      
      public function cleanWingsYes() : void
      {
         this.cleanWings();
      }
      
      public function onClearEnginesClick(param1:MouseEvent) : void
      {
         var count:int = this.countPartsByType(F4DataOrg.TYPE_ENGINE);
         if(count == 0)
         {
            return;
         }
         new UIReally(this.cleanEnginesYes, null, null, "Clear Engines?", "\nStrip all " + count + " thrusters to inventory?", _sp, Attach.attach("UI_Really") as MovieClip, "UI_Really", _ui, this);
      }
      
      public function cleanEnginesYes() : void
      {
         this.cleanEngines();
      }
      
      public function countPartsByType(param1:int) : int
      {
         var count:int = 0;
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.f4datax.plMc)
         {
            if(_loc1_.isRemovableType(param1))
            {
               count++;
            }
         }
         return count;
      }
      
      public function cleanPartsByType(param1:int) : int
      {
         var count:int = 0;
         var targets:Array = [];
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.f4datax.plMc)
         {
            if(_loc1_.isRemovableType(param1))
            {
               targets.push(_loc1_);
            }
         }
         for each(_loc1_ in targets)
         {
            if(this.f4datax.plMc.indexOf(_loc1_) != -1)
            {
               _loc1_.looseLink();
               _loc1_.removeAll(_loc1_, true);
               count++;
            }
         }
         if(count > 0)
         {
            this.form.updatePages();
            this.form.filterX(this.form.filx);
            this.updateNew();
            this.cleanDt();
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
         }
         return count;
      }
      
      public function cleanWeapons() : void
      {
         this.cleanPartsByType(F4DataOrg.TYPE_GUN);
      }
      
      public function cleanWings() : void
      {
         this.cleanPartsByType(F4DataOrg.TYPE_WING);
      }
      
      public function cleanEngines() : void
      {
         this.cleanPartsByType(F4DataOrg.TYPE_ENGINE);
      }
      
      public function initWingHint() : void
      {
         var _showFly:MovieClip = _mov.getChildByName("showFly") as MovieClip;
         if(!_showFly)
         {
            return;
         }
         this.txtWingHint = new TextField();
         this.txtWingHint.name = "txtWingHint";
         UIStyles.applyFormat(this.txtWingHint, UIStyles.FONT_BODY, UIStyles.SIZE_HUD, UIStyles.COLOR_SYSTEM_LIGHT, true, TextFieldAutoSize.LEFT);
         this.txtWingHint.text = "[R] Rotate Wing   |   [F] Flip Wing";
         this.txtWingHint.autoSize = TextFieldAutoSize.LEFT;
         this.txtWingHint.selectable = false;
         this.txtWingHint.mouseEnabled = false;
         UIStyles.applyGlow(this.txtWingHint, 0x000000, 0.9, 4, 3);
         var _pic:MovieClip = _showFly.getChildByName("pic") as MovieClip;
         if(_pic)
         {
            this.txtWingHint.x = _pic.x + 8;
            this.txtWingHint.y = _pic.y + _pic.height - 24;
         }
         else
         {
            this.txtWingHint.x = 10;
            this.txtWingHint.y = 230;
         }
         _showFly.addChild(this.txtWingHint);
      }
      
      public function initWingsInDtTemp() : void
      {
         var dtTemp:MovieClip = _mov.getChildByName("dtTemp") as MovieClip;
         if(!dtTemp || dtTemp.getChildByName("txtWings"))
         {
            return;
         }
         var _i:int = 0;
         while(_i < dtTemp.numChildren)
         {
            var _ch:DisplayObject = dtTemp.getChildAt(_i);
            if(_ch && (_ch.y >= 60 || _ch.getBounds(dtTemp).y >= 60))
            {
               _ch.y += 16.5;
            }
            _i++;
         }
         dtTemp.scaleY = 0.835;
         
         var wingsBg:Shape = new Shape();
         wingsBg.name = "wingsBg";
         var mat:Matrix = new Matrix();
         mat.createGradientBox(105, 16, Math.PI / 2, 0, 64.4);
         wingsBg.graphics.lineStyle(1, 0x2b3744, 0.7);
         wingsBg.graphics.beginGradientFill(GradientType.LINEAR, [0x161e28, 0x090c10], [0.95, 0.95], [0, 255], mat);
         wingsBg.graphics.drawRoundRect(-0.5, 64.4, 105, 15.5, 3, 3);
         wingsBg.graphics.endFill();
         dtTemp.addChildAt(wingsBg, 0);
         
         var lblWings:TextField = new TextField();
         lblWings.name = "lblWings";
         var tfLbl:TextFormat = new TextFormat("Arial", 10, 0xbfbfbf, true);
         tfLbl.align = TextFormatAlign.RIGHT;
         lblWings.defaultTextFormat = tfLbl;
         lblWings.selectable = false;
         lblWings.mouseEnabled = false;
         lblWings.text = "Wings";
         lblWings.x = -2;
         lblWings.y = 67.8;
         lblWings.width = 49;
         lblWings.height = 16;
         dtTemp.addChild(lblWings);
         
         this.txtWings = new TextField();
         this.txtWings.name = "txtWings";
         var tfVal:TextFormat = new TextFormat("Arial", 10, 0xbfbfbf, true);
         tfVal.align = TextFormatAlign.CENTER;
         this.txtWings.defaultTextFormat = tfVal;
         this.txtWings.selectable = false;
         this.txtWings.mouseEnabled = false;
         this.txtWings.text = "";
         this.txtWings.x = 53.05;
         this.txtWings.y = 67.8;
         this.txtWings.width = 48.5;
         this.txtWings.height = 16;
         dtTemp.addChild(this.txtWings);
         
         this.updateWingsInDtTemp();
      }
      
      public function updateWingsInDtTemp(param1:F4ShowData = null) : void
      {
         if(!this.txtWings)
         {
            return;
         }
         var _dtTemp:MovieClip = _mov.getChildByName("dtTemp") as MovieClip;
         var _txtHp:TextField = _dtTemp ? _dtTemp.getChildByName("txtHp") as TextField : null;
         if(!_txtHp || _txtHp.text == "" || _txtHp.text == "-")
         {
            if(this.txtWings.text != "")
            {
               this.txtWings.text = "";
            }
            return;
         }
         var _curWings:int = 0;
         var _maxWings:int = 0;
         if(param1)
         {
            _curWings = param1.wings;
            _maxWings = param1.maxWings;
         }
         else
         {
            var _hull:F4DataOrg = F4DataHole.getAssemblyHull();
            if(!_hull)
            {
               if(this.txtWings.text != "")
               {
                  this.txtWings.text = "";
               }
               return;
            }
            _curWings = F4DataHole.countMountedWings();
            _maxWings = F4DataOrg.maxWingsForHull(_hull);
         }
         var _text:String = _curWings + "/" + _maxWings;
         if(this.txtWings.text != _text)
         {
            this.txtWings.text = _text;
         }
         var _color:uint = _curWings >= _maxWings ? 0xffcc00 : 0xbfbfbf;
         if(_curWings > _maxWings)
         {
            _color = 0xff4444;
         }
         var _tf:TextFormat = this.txtWings.defaultTextFormat;
         _tf.align = TextFormatAlign.CENTER;
         if(_tf.color != _color)
         {
            _tf.color = _color;
            this.txtWings.defaultTextFormat = _tf;
            this.txtWings.setTextFormat(_tf);
         }
      }
      
      override public function afterFadeIn() : void
      {
         this.updateNew();
         this.form.filterX(4);
         this.form.initsortMC();
         this.form.sort(0);
         this.form.sort(0);
         this.formFly.initsortMC();
         this.formFly.sort(0);
         this.formFly.sort(0);
         if(ConfigAndStory.SHOWTEXTMENU[2] == 0)
         {
            ConfigAndStory.DIRECTOR.beforePlace(this.afterTalk,this,0,0,0);
            ConfigAndStory.SHOWTEXTMENU[2] = 1;
            ConfigAndStory.saveData();
         }
         else
         {
            this.afterTalk();
         }
         this.addBtnL();
      }
      
      public function afterTalk() : void
      {
         super.afterFadeIn();
         if(ConfigAndStory.HOWTOPLAY[3] == 0)
         {
            new UIHelpX2(3,"How to build a craft",_sp,Attach.attach("UI_HelpBuildCraft") as MovieClip,uiName,_ui,this);
         }
      }
      
      override public function removeSelf() : UIBase
      {
         super.removeSelf();
         return _last;
      }
      
      public function changeOrgDt(param1:OrgEvent) : void
      {
         this.addDt(param1.org);
      }
      
      override public function removeBtnL() : void
      {
         _mov.removeEventListener(Event.ENTER_FRAME,this.uiloop);
         this.uix.removeEventListener("Org",this.changeOrgDt);
         super.removeBtnL();
      }
      
      override public function addBtnL() : void
      {
         _mov.addEventListener(Event.ENTER_FRAME,this.uiloop);
         this.uix.addEventListener("Org",this.changeOrgDt);
         super.addBtnL();
      }
      
      public function addDt(param1:F4DataOrg) : void
      {
         var _loc2_:MovieClip = null;
         if(this.dtOrg != param1)
         {
            _loc2_ = _mov.getChildByName("dt") as MovieClip;
            ShowPart.addPart(param1,_loc2_,InitData.SELL);
            this.dtOrg = param1;
            this.hidePoints();
            this.showPoint(param1);
         }
      }
      
      public function cleanDt() : void
      {
         ShowPart.cleanDt(_mov.getChildByName("dt") as MovieClip);
         this.dtOrg = null;
         this.hidePoints();
         this.updateClearButtons();
      }
      
      public function addDtL(param1:F4DataLinks) : void
      {
         var _loc2_:MovieClip = null;
         var _loc3_:F4ShowData = null;
         if(this.dtLink != param1)
         {
            _loc2_ = _mov.getChildByName("showDetail") as MovieClip;
            _loc3_ = param1.createShowData(InitData.SELL);
            _loc3_.makeFlyClone();
            ShowPart.addFly(_loc3_,_loc2_);
            this.dtLink = param1;
         }
      }
      
      public function cleanDtL() : void
      {
         ShowPart.cleanFly(_mov.getChildByName("showDetail") as MovieClip);
         this.dtLink = null;
      }
      
      public function cleanDtTemp() : void
      {
         ShowPart.cleanFlyTemp(_mov.getChildByName("dtTemp") as MovieClip);
         if(this.txtWings)
         {
            this.txtWings.text = "";
         }
      }
      
      public function addDtLTempRun() : void
      {
         var _loc2_:MovieClip = _mov.getChildByName("dtTemp") as MovieClip;
         var _loc3_:F4ShowData = F4DataLinks.calculateCraftStats(this.f4datax);
         if(_loc3_)
         {
            ShowPart.addFlyTemp(_loc3_,_loc2_);
         }
         else
         {
            ShowPart.cleanFlyTemp(_loc2_);
         }
         this.updateWingsInDtTemp(_loc3_);
      }
      
      private function runzoom() : void
      {
         if(Math.abs(this.tempZoom - this.zoomNum) > 0.001)
         {
            this.tempZoom += (this.zoomNum - this.tempZoom) * 0.1;
            if(Math.abs(this.tempZoom - this.zoomNum) <= 0.001)
            {
               this.tempZoom = this.zoomNum;
            }
            this.uix.scaleX = this.uix.scaleY = this.tempZoom;
         }
      }
      
      private function uiloop(param1:Event) : void
      {
         this.addDtLTempRun();
         this.runzoom();
      }
      
      override public function outAct(param1:String) : void
      {
         if(!this.formFly.form.visible)
         {
            if(param1.indexOf("form") != -1)
            {
               this.form.disableLight();
            }
         }
         else if(param1.indexOf("form") != -1)
         {
            this.formFly.disableLight();
         }
      }
      
      override public function overAct(param1:String) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataOrg = null;
         var _loc4_:F4DataLinks = null;
         if(!this.formFly.form.visible)
         {
            if(param1.indexOf("form") != -1)
            {
               _loc2_ = parseInt(param1.substr(4));
               _loc3_ = this.form.getLink(_loc2_);
               if(_loc3_)
               {
                  this.addDt(_loc3_);
                  this.form.lightOn(_loc2_);
               }
            }
            else if(param1.indexOf("chooseu") != -1)
            {
               _loc2_ = parseInt(param1.substr(7));
               if(this.undoList[_loc2_])
               {
                  this.addDt(this.undoList[_loc2_]);
               }
            }
         }
         else if(param1.indexOf("form") != -1)
         {
            _loc2_ = parseInt(param1.substr(4));
            _loc4_ = this.formFly.getLink(_loc2_);
            if(_loc4_)
            {
               this.addDtL(_loc4_);
               this.formFly.lightOn(_loc2_);
            }
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc3_:int = 0;
         var _loc4_:int = 0;
         var _loc5_:String = null;
         var _loc6_:F4DataLinks = null;
         var _loc7_:String = null;
         var _loc8_:F4DataLinks = null;
         var _loc9_:F4DataLinks = null;
         var _loc10_:String = null;
         var _loc11_:F4DataOrg = null;
         var _loc2_:String = param1.name;
         if(_loc2_ == "zin")
         {
            this.zoomNum *= 1.25;
            if(this.zoomNum > 4)
            {
               this.zoomNum = 4;
            }
            else
            {
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[12]);
            }
         }
         else if(_loc2_ == "help")
         {
            if(this.formFly.form.visible)
            {
               new UIHelpX2(4,"Crafts",_sp,Attach.attach("UI_HelpBreak") as MovieClip,uiName,_ui,this);
            }
            else if(this.nowedit)
            {
               new UIHelpX2(3,"How to edit a craft",_sp,Attach.attach("UI_HelpEdit") as MovieClip,uiName,_ui,this);
            }
            else
            {
               new UIHelpX2(3,"How to build a craft",_sp,Attach.attach("UI_HelpBuildCraft") as MovieClip,uiName,_ui,this);
            }
         }
         else if(_loc2_ == "zout")
         {
            this.zoomNum *= 0.75;
            if(this.zoomNum < 0.5)
            {
               this.zoomNum = 0.5;
            }
            else
            {
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[16]);
            }
         }
         else if(_loc2_ == "fly")
         {
            if(this.f4datax.plMc.length > 0)
            {
               if(!this.formFly.form.visible)
               {
                  if(this.formFly.selToDel >= 0)
                  {
                     this.mergeFlyReal = new UIReally(this.mergeFlyLeaveYes,this.mergeFlyLeaveNo,this.nowNameFly,"Merge?","\nMerge the craft and leave?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
                  }
                  else
                  {
                     new UIReally(this.leaveYes,this.leaveNo,null,"Cancel?","\nCancel and leave?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
                  }
               }
               else
               {
                  if(this.formFly.selToDel >= 0)
                  {
                     this.cleanMCNotBack();
                  }
                  this.leaveNormal();
               }
            }
            else
            {
               this.leaveNormal();
            }
         }
         if(this.formFly.form.visible)
         {
            if(_loc2_ == "next")
            {
               this.formFly.next(true);
            }
            else if(_loc2_ == "prev")
            {
               this.formFly.next(false);
            }
            else if(_loc2_ == "first")
            {
               this.formFly.toPage(1);
            }
            else if(_loc2_ == "last")
            {
               this.formFly.toPage(this.formFly.allPage);
            }
            else if(_loc2_ == "break")
            {
               _loc5_ = param1.parentName;
               _loc3_ = parseInt(_loc5_.substr(5));
               _loc6_ = this.formFly.getLink(_loc3_);
               if(_loc6_)
               {
                  this.cleanDtL();
                  this.cleanMCNotBack();
                  this.makeFly(_loc6_);
                  this.hidePoints();
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                  this.formFly.selToDel = this.formFly.now;
                  new UIReally(this.removeYes,this.removeNo,null,"Break?","\nBreak " + _loc6_.name + " to parts?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
               }
            }
            else if(_loc2_ == "part")
            {
               this.formFly.form.visible = false;
               this.form.form.visible = true;
               _mov.getChildByName("tmask").visible = false;
               this.showFlysX(false);
               this.showFlyEdit(false);
               disableBtn("new");
               this.cleanDtL();
               this.cleanMCNotBack();
               this.showFlys(true);
            }
            else if(_loc2_ == "edit")
            {
               trace("EDIT!");
               _loc7_ = param1.parentName;
               _loc3_ = parseInt(_loc7_.substr(5));
               _loc8_ = this.formFly.getLink(_loc3_);
               if(_loc8_)
               {
                  this.formFly.selToDel = this.formFly.now;
                  this.cleanDtL();
                  this.cleanMCNotBack();
                  this.makeFly(_loc8_);
                  this.autoZoom();
                  this.hidePoints();
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                  new UIReally(this.editYes,null,null,"Edit?","\nWanna edit " + _loc8_.name + " ?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
               }
            }
            else if(_loc2_.indexOf("choosesort") != -1)
            {
               _loc4_ = parseInt(_loc2_.substr(10));
               this.formFly.sort(_loc4_);
            }
            else if(_loc2_.indexOf("form") != -1)
            {
               _loc3_ = parseInt(_loc2_.substr(4));
               _loc9_ = this.formFly.getLink(_loc3_);
               if(_loc9_)
               {
                  this.formFly.selToDel = this.formFly.now;
                  this.cleanDtL();
                  this.cleanMCNotBack();
                  this.makeFly(_loc9_);
                  this.autoZoom();
                  this.hidePoints();
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
               }
            }
         }
         else if(_loc2_ == "next")
         {
            this.form.next(true);
         }
         else if(_loc2_ == "prev")
         {
            this.form.next(false);
         }
         else if(_loc2_ == "first")
         {
            this.form.toPage(1);
         }
         else if(_loc2_ == "last")
         {
            this.form.toPage(this.form.allPage);
         }
         else if(_loc2_ == "add")
         {
            if(this.part2In())
            {
               this.addFlyReal = new UIReally(this.addFlyYes,this.addFlyNo,"CF-" + this.f4datax.plFly.length,"Add Craft?","\nInput the name of it.",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
         }
         else if(_loc2_ == "merge")
         {
            if(this.part2In())
            {
               this.mergeFlyReal = new UIReally(this.mergeFlyYes,this.mergeFlyNo,this.nowNameFly,"Merge?","\nMerge the craft?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
         }
         else if(_loc2_ == "new")
         {
            if(this.f4datax.plMc.length > 0)
            {
               _loc10_ = "\nBreak these to parts?";
               new UIReally(this.newYes,null,null,"New?",_loc10_,_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
         }
         else if(_loc2_ == "flyF")
         {
            if(this.f4datax.plMc.length > 0)
            {
               new UIReally(this.toflyYes,this.toflyNo,null,"Cancel?","\nBreak these to parts?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
            else
            {
               this.toflyYes();
            }
         }
         else if(_loc2_.indexOf("choosesort") != -1)
         {
            _loc4_ = parseInt(_loc2_.substr(10));
            this.form.sort(_loc4_);
         }
         else if(_loc2_.indexOf("form") != -1)
         {
            _loc3_ = parseInt(_loc2_.substr(4));
            _loc11_ = this.form.getLink(_loc3_);
            if(_loc11_)
            {
               this.addPart(_loc11_);
               this.cleanDt();
               this.hidePoints();
               this.undoCk();
            }
            _loc11_ = this.form.getLink(_loc3_);
            if(_loc11_)
            {
               this.addDt(_loc11_);
               this.form.lightOn(_loc3_);
            }
         }
         else if(_loc2_.indexOf("choosefil") != -1)
         {
            _loc3_ = parseInt(_loc2_.substr(9));
            this.form.lastSort = -1;
            this.form.sortMC();
            this.form.filterX(_loc3_);
            this.form.initsortMC();
            this.form.sort(0);
            this.form.sort(0);
         }
         else if(_loc2_.indexOf("chooseu") != -1)
         {
            _loc3_ = parseInt(_loc2_.substr(7));
            if(this.undoList[_loc3_])
            {
               this.addPart(this.undoList[_loc3_]);
               this.cleanDt();
               this.hidePoints();
               this.undoUse(_loc3_);
            }
         }
      }
      
      public function editYes() : void
      {
         this.formFly.form.visible = false;
         this.form.form.visible = true;
         _mov.getChildByName("tmask").visible = false;
         this.showFlysX(false);
         disableBtn("new");
         this.showFlys(false);
         this.showFlyEdit(true);
         this.nowedit = true;
         this.updateClearButtons();
      }
      
      public function showFlys(param1:Boolean) : void
      {
         this.form.form.getChildByName("$add").visible = param1;
         this.form.form.getChildByName("$merge").visible = !param1;
         if(!param1)
         {
            disableBtn("flyF");
         }
         else
         {
            enableBtn("flyF");
         }
      }
      
      public function showFlysX(param1:Boolean) : void
      {
         _mov.getChildByName("dtTemp").visible = !param1;
         _mov.getChildByName("dt").visible = !param1;
         _mov.getChildByName("showDetail").visible = param1;
         this.uix.mouseChildren = !param1;
         this.uix.mouseEnabled = !param1;
         if(this.txtWingHint)
         {
            this.txtWingHint.visible = !param1;
         }
         if(param1)
         {
            (_mov.getChildByName("txtNew") as TextField).text = "";
         }
      }
      
      public function showFlyEdit(param1:Boolean) : void
      {
         if(param1)
         {
            (_mov.getChildByName("txtNew") as TextField).text = "Edit";
         }
         else
         {
            (_mov.getChildByName("txtNew") as TextField).text = "New";
         }
      }
      
      public function addFlyYes() : void
      {
         this.addToFly(false,this.addFlyReal.inputWord);
         this.addFlyReal = null;
         this.cleanDt();
         ConfigAndStory.ckAI();
         ConfigAndStory.saveData();
      }
      
      public function addFlyNo() : void
      {
         this.addFlyReal = null;
      }
      
      public function mergeFlyYes() : void
      {
         this.addToFly(true,this.mergeFlyReal.inputWord);
         this.mergeFlyReal = null;
         this.showFlys(true);
         this.showFlyEdit(false);
         this.updateNew();
         this.formFly.selToDel = -1;
         this.formFly.now = -1;
         this.nowedit = false;
         ConfigAndStory.saveData();
      }
      
      public function mergeFlyNo() : void
      {
         this.mergeFlyReal = null;
      }
      
      public function resetYes() : void
      {
         this.resetMC();
         this.cleanDt();
      }
      
      public function newYes() : void
      {
         this.cleanMC();
         this.cleanDt();
         this.showFlys(true);
         this.showFlyEdit(false);
         this.updateNew();
      }
      
      public function mergeFlyLeaveYes() : void
      {
         var tempui:UIBase = null;
         this.addToFly(true,this.mergeFlyReal.inputWord);
         this.mergeFlyReal = null;
         this.cleanMCNotBack();
         this.removeSelf();
         tempui = this;
         allRemoved = function():void
         {
            MakeUI.makeChoose(tempui,_sp,_ui);
         };
         this.cleanDt();
         ConfigAndStory.saveData();
      }
      
      public function mergeFlyLeaveNo() : void
      {
         this.mergeFlyReal = null;
      }
      
      public function leaveYes() : void
      {
         var tempui:UIBase = null;
         this.cleanMC();
         this.removeSelf();
         tempui = this;
         allRemoved = function():void
         {
            MakeUI.makeChoose(tempui,_sp,_ui);
         };
      }
      
      public function leaveNormal() : void
      {
         var tempui:UIBase = null;
         this.removeSelf();
         tempui = this;
         allRemoved = function():void
         {
            MakeUI.makeChoose(tempui,_sp,_ui);
         };
      }
      
      public function leaveNo() : void
      {
      }
      
      public function toflyYes() : void
      {
         this.cleanMC();
         this.formFly.init();
         this.formFly.form.visible = true;
         this.form.form.visible = false;
         _mov.getChildByName("tmask").visible = true;
         this.showFlysX(true);
         this.cleanDt();
         this.formFly.selToDel = -1;
         this.formFly.now = -1;
         this.formFly.lastSort = -1;
         this.formFly.sort(0);
         this.formFly.sort(0);
         if(ConfigAndStory.HOWTOPLAY[4] == 0)
         {
            new UIHelpX2(4,"Crafts",_sp,Attach.attach("UI_HelpBreak") as MovieClip,uiName,_ui,this);
         }
      }
      
      public function toflyNo() : void
      {
      }
      
      public function removeYes() : void
      {
         this.cleanMC();
         this.formFly.deleteNow();
         this.formFly.selToDel = -1;
         this.formFly.now = -1;
         ConfigAndStory.ckAI();
      }
      
      public function removeNo() : void
      {
      }
      
      public function updateNew() : void
      {
         if(this.f4datax.plMc.length == 0)
         {
            disableBtn("new");
            disableBtn("add");
         }
         else
         {
            enableBtn("new");
            enableBtn("add");
         }
         this.updateClearButtons();
      }
      
      public function cleanMCNotBack() : void
      {
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.f4datax.plMc)
         {
            if(_loc1_.f4DataLink.f4dataorg.useAs == 2)
            {
               _loc1_.removeAllNotBack(_loc1_,true);
               break;
            }
         }
         while(this.f4datax.plMc.length > 0)
         {
            (this.f4datax.plMc[0] as F4PartMC).removeAllNotBack(this.f4datax.plMc[0] as F4PartMC,false);
         }
         this.updateNew();
      }
      
      public function cleanMC() : void
      {
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.f4datax.plMc)
         {
            if(_loc1_.f4DataLink.f4dataorg.useAs == 2)
            {
               _loc1_.removeAll(_loc1_);
               break;
            }
         }
         while(this.f4datax.plMc.length > 0)
         {
            (this.f4datax.plMc[0] as F4PartMC).removeAll(this.f4datax.plMc[0] as F4PartMC,false);
         }
         this.form.updatePages();
         this.updateNew();
      }
      
      public function resetMC() : void
      {
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.f4datax.plMc)
         {
            if(_loc1_.f4DataLink.f4dataorg.useAs == 2)
            {
               _loc1_.removeAllNoFly(_loc1_);
               break;
            }
         }
         this.form.updatePages();
      }
      
      public function get nowNameFly() : String
      {
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.f4datax.plMc)
         {
            if(_loc1_.f4DataLink.f4dataorg.useAs == 2)
            {
               return _loc1_.f4DataLink.name;
            }
         }
         return null;
      }
      
      public function addToFly(param1:Boolean, param2:String) : void
      {
         var _loc3_:F4PartMC = null;
         for each(_loc3_ in this.f4datax.plMc)
         {
            if(_loc3_.f4DataLink.f4dataorg.useAs == 2)
            {
               _loc3_.f4DataLink.name = param2;
               if(!param1)
               {
                  _loc3_.f4DataLink.index = this.f4datax.plFly.length;
                  this.f4datax.plFly.push(_loc3_.f4DataLink);
               }
               else if(_loc3_.f4DataLink.index != -1)
               {
                  this.f4datax.plFly[_loc3_.f4DataLink.index] = _loc3_.f4DataLink;
               }
               else
               {
                  _loc3_.f4DataLink.index = this.f4datax.plFly.length;
                  this.f4datax.plFly.push(_loc3_.f4DataLink);
               }
               break;
            }
         }
         this.cleanMCNotBack();
      }
      
      public function addPart(param1:F4DataOrg) : void
      {
         var _loc4_:MovieClip = null;
         var _loc5_:F4PartMC = null;
         var _loc6_:Sprite = null;
         var _loc2_:F4DataOrg = param1;
         var _loc3_:Boolean = this.part2In();
         if(!_loc3_ && _loc2_.type == 0)
         {
            _loc3_ = true;
         }
         else
         {
            _loc3_ = false;
         }
         if(_loc3_ || this.partCanPut(_loc2_))
         {
            _loc4_ = Attach.attach(_loc2_.mcName) as MovieClip;
            if(_loc2_.type == 0)
            {
               _loc6_ = this.ui_body;
            }
            else if(_loc2_.type == 1)
            {
               _loc6_ = this.ui_wing;
            }
            else
            {
               _loc6_ = this.ui_other;
            }
            _loc5_ = this.mc2f4.mc2Data(_loc4_,_loc6_,_loc2_,_loc3_,_loc2_.lr);
            _loc5_.allf4 = this.f4datax.plMc;
            _loc5_.allf4.push(_loc5_);
            _loc5_.form = this.form;
            _loc5_.undo = this;
            _loc5_.delPartAct = this.cleanDt;
            this.autoPlug(_loc5_);
            this.form.filter();
            if(!this.nowedit)
            {
               this.updateNew();
            }
            else
            {
               this.updateClearButtons();
            }
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
         }
         else
         {
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[13]);
         }
      }
      
      public function part2In() : Boolean
      {
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.f4datax.plMc)
         {
            if(_loc1_.f4DataLink.f4dataorg.useAs == 2)
            {
               return true;
            }
         }
         return false;
      }
      
      public function autoZoom() : void
      {
         var _loc1_:Number = this.uix.width / this.uix.scaleX;
         var _loc2_:Number = this.uix.height / this.uix.scaleY;
         var _loc3_:Number = _loc1_ > _loc2_ ? _loc1_ : _loc2_;
         var _loc4_:Number = 250 / _loc3_;
         this.zoomNum = _loc4_;
         if(this.zoomNum < 0.5)
         {
            this.zoomNum = 0.5;
         }
         else if(this.zoomNum > 4)
         {
            this.zoomNum = 4;
         }
      }
      
      public function makeFly(param1:F4DataLinks) : void
      {
         var _loc2_:Sprite = null;
         var _loc3_:Boolean = false;
         var _loc4_:MovieClip = null;
         var _loc5_:F4PartMC = null;
         var _loc6_:F4DataHole = null;
         if(param1.f4dataorg.type == 0)
         {
            _loc2_ = this.ui_body;
         }
         else if(param1.f4dataorg.type == 1)
         {
            _loc2_ = this.ui_wing;
         }
         else
         {
            _loc2_ = this.ui_other;
         }
         _loc3_ = param1.f4dataorg.useAs == 2;
         _loc4_ = Attach.attach(param1.f4dataorg.mcName) as MovieClip;
         _loc5_ = this.mc2f4.mc2Data(_loc4_,_loc2_,param1.f4dataorg,_loc3_,param1.f4dataorg.lr);
         _loc5_.f4DataLink = param1;
         _loc5_.f4DataLink.f4mc = _loc5_;
         _loc5_.allf4 = this.f4datax.plMc;
         _loc5_.allf4.push(_loc5_);
         _loc5_.form = this.form;
         _loc5_.undo = this;
         _loc5_.delPartAct = this.cleanDt;
         for each(_loc6_ in param1.f4dataorg.hole)
         {
            if(_loc6_.partf4)
            {
               this.makeFly(_loc6_.partf4);
            }
         }
         if(param1.f4dataorg.useAs == 2)
         {
            _loc5_.reLinkParent();
         }
      }
      
      public function autoPlug(param1:F4PartMC) : Boolean
      {
         var _loc2_:F4PartMC = null;
         var _loc3_:F4DataHole = null;
         for each(_loc2_ in this.f4datax.plMc)
         {
            if(param1 != _loc2_ && _loc2_.f4DataLink.parentPart != param1.f4DataLink)
            {
               for each(_loc3_ in _loc2_.f4DataLink.f4dataorg.hole)
               {
                  if(!_loc3_.partf4 && _loc3_.acceptable(param1.f4DataLink) == 0)
                  {
                     _loc3_.partf4 = param1.f4DataLink;
                     param1.f4DataLink.atHole = _loc3_;
                     param1.f4DataLink.parentPart = _loc2_.f4DataLink;
                     if(param1.f4DataLink.f4dataorg.type == 1 && _loc3_.lr != 2)
                     {
                        param1.setLR(_loc3_.lr);
                     }
                     param1.allFinalMove(param1);
                     return true;
                  }
               }
            }
         }
         return false;
      }
      
      public function hidePoints() : void
      {
         var _loc1_:MovieClip = null;
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc4_:DisplayObject = null;
         var _loc5_:String = null;
         var _loc6_:F4PartMC = null;
         for each(_loc6_ in this.f4datax.plMc)
         {
            _loc1_ = _loc6_.mc;
            _loc3_ = _loc1_.numChildren;
            _loc2_ = 0;
            while(_loc2_ < _loc3_)
            {
               _loc4_ = _loc1_.getChildAt(_loc2_);
               _loc5_ = _loc4_.name;
               if(_loc5_.charAt(0) == "L")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "H")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "F")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "P")
               {
                  _loc4_.visible = false;
               }
               _loc2_++;
            }
         }
      }
      
      public function partCanPut(param1:F4DataOrg) : Boolean
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataHole = null;
         var _loc4_:F4PartMC = null;
         for each(_loc4_ in this.f4datax.plMc)
         {
            _loc2_ = 0;
            while(_loc2_ < _loc4_.f4DataLink.f4dataorg.hole.length)
            {
               _loc3_ = _loc4_.f4DataLink.f4dataorg.hole[_loc2_];
               if(!_loc3_.partf4 && _loc3_.acceptableOrg(param1) == 0)
               {
                  this.showHole(_loc2_,_loc4_.mc);
                  return true;
               }
               _loc2_++;
            }
         }
         return false;
      }
      
      public function showPoint(param1:F4DataOrg) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataHole = null;
         var _loc4_:F4PartMC = null;
         for each(_loc4_ in this.f4datax.plMc)
         {
            _loc2_ = 0;
            while(_loc2_ < _loc4_.f4DataLink.f4dataorg.hole.length)
            {
               _loc3_ = _loc4_.f4DataLink.f4dataorg.hole[_loc2_];
               if(!_loc3_.partf4 && _loc3_.acceptableOrg(param1) == 0)
               {
                  this.showHole(_loc2_,_loc4_.mc);
               }
               _loc2_++;
            }
         }
      }
      
      public function showHole(param1:int, param2:MovieClip) : void
      {
         var _loc3_:int = 0;
         var _loc4_:int = 0;
         var _loc5_:DisplayObject = null;
         var _loc6_:String = null;
         var _loc7_:int = -1;
         _loc4_ = param2.numChildren;
         _loc3_ = 0;
         while(_loc3_ < _loc4_)
         {
            _loc5_ = param2.getChildAt(_loc3_);
            _loc6_ = _loc5_.name;
            if(_loc6_.charAt(0) == "H")
            {
               if(++_loc7_ == param1)
               {
                  _loc5_.visible = true;
               }
            }
            _loc3_++;
         }
      }
      
      public function addToUndo(param1:F4DataOrg) : void
      {
         var _loc2_:* = int(this.undoNum - 1);
         while(_loc2_ > 0)
         {
            this.undoList[_loc2_] = this.undoList[_loc2_ - 1];
            _loc2_--;
         }
         this.undoList[0] = param1;
         this.undoShow();
         this.updateNew();
      }
      
      public function undoSortAfterUse(param1:int) : void
      {
         var _loc2_:int = param1;
         while(_loc2_ < this.undoNum - 1)
         {
            this.undoList[_loc2_] = this.undoList[_loc2_ + 1];
            _loc2_++;
         }
         this.undoList[this.undoNum - 1] = null;
         this.undoShow();
      }
      
      public function undoShow() : void
      {
         var _loc1_:F4DataOrg = null;
         var _loc2_:int = 0;
         while(_loc2_ < this.undoNum)
         {
            if(this.undoList[_loc2_])
            {
               _loc1_ = this.undoList[_loc2_];
               (_mov.getChildByName("$chooseu" + _loc2_) as TextField).text = ConfigAndStory.num2Type(_loc1_.type).charAt(0);
            }
            else
            {
               (_mov.getChildByName("$chooseu" + _loc2_) as TextField).text = "-";
            }
            _loc2_++;
         }
      }
      
      public function undoUse(param1:int) : void
      {
         var _loc2_:F4DataOrg = this.undoList[param1];
         if(Boolean(_loc2_) && _loc2_.useAs > 0)
         {
            this.undoList[param1] = null;
            this.undoSortAfterUse(param1);
         }
      }
      
      public function undoCk() : void
      {
         var _loc1_:F4DataOrg = null;
         var _loc2_:int = 0;
         while(_loc2_ < this.undoNum)
         {
            if(this.undoList[_loc2_])
            {
               _loc1_ = this.undoList[_loc2_];
               if(_loc1_.useAs > 0)
               {
                  this.undoList[_loc2_] = null;
                  this.undoSortAfterUse(_loc2_);
               }
            }
            _loc2_++;
         }
      }
   }
}

