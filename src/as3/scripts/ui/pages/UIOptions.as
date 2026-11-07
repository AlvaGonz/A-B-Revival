package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.GradientType;
   import flash.display.Graphics;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import flash.filters.GlowFilter;
   import flash.geom.Matrix;
   import flash.text.TextField;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.base.UIStyles;
   
   public class UIOptions extends UIBase
   {
      
      public var config:Array = ConfigAndStory.CONFIG.concat();
      
      public var selectedDisplayMode:int = 0;
      
      private var btnRes:Sprite;
      
      private var txtRes:TextField;
      
      private var isResHovered:Boolean = false;

      private var btnControls:Sprite;

      private var txtControls:TextField;

      private var isControlsHovered:Boolean = false;
      
      private static const DISPLAY_MODES:Array = [
         "Resolution: 640 x 480",
         "Resolution: 1280 x 720 (HD)",
         "Resolution: 1920 x 1080 (FHD)",
         "Display: Fullscreen"
      ];
      
      public function UIOptions(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         _mov = param2;
         this.updateChoose();
         if(ConfigAndStory.FULLSCREEN)
         {
            this.selectedDisplayMode = 3;
         }
         else if(ConfigAndStory.RESOLUTION == ConfigAndStory.RES_1080)
         {
            this.selectedDisplayMode = 2;
         }
         else if(ConfigAndStory.RESOLUTION == ConfigAndStory.RES_720)
         {
            this.selectedDisplayMode = 1;
         }
         else
         {
            this.selectedDisplayMode = 0;
         }
         this.initResolutionUI();
         this.initControlsUI();
         super(param1,param2,param3,param4,param5);
      }
      
      private function initResolutionUI() : void
      {
         var hiMC:DisplayObject = _mov.getChildByName("$choosehi");
         var targetX:Number = 318;
         var targetY:Number = 244.8;
         if(hiMC)
         {
            targetX = hiMC.x;
            targetY = hiMC.y;
            hiMC.visible = false;
         }
         
         this.btnRes = new Sprite();
         this.btnRes.buttonMode = true;
         this.btnRes.useHandCursor = true;
         this.btnRes.x = targetX;
         this.btnRes.y = targetY;
         
         this.txtRes = new TextField();
         this.txtRes.width = 180;
         this.txtRes.height = 20;
         this.txtRes.x = -90;
         this.txtRes.y = -9;
         this.txtRes.selectable = false;
         this.txtRes.mouseEnabled = false;
         
         var tf:TextFormat = new TextFormat("Arial", 11, 0xffffff, true);
         tf.align = TextFormatAlign.CENTER;
         this.txtRes.defaultTextFormat = tf;
         this.txtRes.filters = [new GlowFilter(0x000000, 0.8, 3, 3, 2, 1)];
         
         this.btnRes.addChild(this.txtRes);
         this.btnRes.addEventListener(MouseEvent.MOUSE_OVER, this.onResOver);
         this.btnRes.addEventListener(MouseEvent.MOUSE_OUT, this.onResOut);
         this.btnRes.addEventListener(MouseEvent.CLICK, this.onResClick);
         
         _mov.addChild(this.btnRes);
         this.updateResolutionUI();
      }
      
      private function updateResolutionUI() : void
      {
         var g:Graphics = this.btnRes.graphics;
         g.clear();
         g.beginFill(0x08080a, 0.95);
         
         if(this.isResHovered)
         {
            g.lineStyle(1.0, 0x95e0f2, 1.0);
         }
         else
         {
            var mat:Matrix = new Matrix();
            mat.createGradientBox(180, 22, 0, -90, -11);
            g.lineStyle(1.0);
            g.lineGradientStyle(GradientType.LINEAR, [0xffffff, 0xabbfdc, 0xffffff], [0.15, 0.5, 0.15], [0, 128, 255], mat);
         }
         
         g.moveTo(85, -11);
         g.lineTo(90, -6);
         g.lineTo(90, 6);
         g.lineTo(85, 11);
         g.lineTo(-85, 11);
         g.lineTo(-90, 6);
         g.lineTo(-90, -6);
         g.lineTo(-85, -11);
         g.lineTo(85, -11);
         g.endFill();
         
         if(this.txtRes)
         {
            this.txtRes.text = DISPLAY_MODES[this.selectedDisplayMode];
            this.txtRes.textColor = this.isResHovered ? 0xffffff : 0xbfd7ea;
         }
      }
      
      private function onResOver(e:MouseEvent) : void
      {
         this.isResHovered = true;
         this.updateResolutionUI();
      }
      
      private function onResOut(e:MouseEvent) : void
      {
         this.isResHovered = false;
         this.updateResolutionUI();
      }
      
      private function onResClick(e:MouseEvent) : void
      {
         if(InitData.SOUNDC2 && InitData.SOUNDDATA && InitData.SOUNDDATA.soundArray && InitData.SOUNDDATA.soundArray.length > 19)
         {
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[19], 1);
         }
         this.selectedDisplayMode = (this.selectedDisplayMode + 1) % DISPLAY_MODES.length;
         this.updateResolutionUI();
      }

      private function initControlsUI() : void
      {
         this.btnControls = new Sprite();
         this.btnControls.buttonMode = true;
         this.btnControls.useHandCursor = true;
         this.btnControls.x = 318;
         this.btnControls.y = 352;

         this.txtControls = new TextField();
         this.txtControls.width = 180;
         this.txtControls.height = 20;
         this.txtControls.x = -90;
         this.txtControls.y = -9;
         this.txtControls.selectable = false;
         this.txtControls.mouseEnabled = false;

         UIStyles.applyFormat(this.txtControls, UIStyles.FONT_BODY, 11, 0xFFFFFF, true, TextFormatAlign.CENTER);
         UIStyles.applyGlow(this.txtControls, 0x000000, 0.8, 3, 2);
         this.txtControls.text = "CONTROLS / KEYBINDS";

         this.btnControls.addChild(this.txtControls);
         this.btnControls.addEventListener(MouseEvent.MOUSE_OVER, this.onControlsOver);
         this.btnControls.addEventListener(MouseEvent.MOUSE_OUT, this.onControlsOut);
         this.btnControls.addEventListener(MouseEvent.CLICK, this.onControlsClick);

         _mov.addChild(this.btnControls);
         this.updateControlsUI();
      }

      private function updateControlsUI() : void
      {
         var g:Graphics = this.btnControls.graphics;
         g.clear();
         g.beginFill(0x08080a, 0.95);

         if(this.isControlsHovered)
         {
            g.lineStyle(1.0, 0x95e0f2, 1.0);
         }
         else
         {
            var mat:Matrix = new Matrix();
            mat.createGradientBox(180, 22, 0, -90, -11);
            g.lineStyle(1.0);
            g.lineGradientStyle(GradientType.LINEAR, [0xffffff, 0xabbfdc, 0xffffff], [0.15, 0.5, 0.15], [0, 128, 255], mat);
         }

         g.moveTo(85, -11);
         g.lineTo(90, -6);
         g.lineTo(90, 6);
         g.lineTo(85, 11);
         g.lineTo(-85, 11);
         g.lineTo(-90, 6);
         g.lineTo(-90, -6);
         g.lineTo(-85, -11);
         g.lineTo(85, -11);
         g.endFill();

         if(this.txtControls)
         {
            this.txtControls.textColor = this.isControlsHovered ? 0xffffff : 0xbfd7ea;
         }
      }

      private function onControlsOver(e:MouseEvent) : void
      {
         this.isControlsHovered = true;
         this.updateControlsUI();
      }

      private function onControlsOut(e:MouseEvent) : void
      {
         this.isControlsHovered = false;
         this.updateControlsUI();
      }

      private function onControlsClick(e:MouseEvent) : void
      {
         if(InitData.SOUNDC2 && InitData.SOUNDDATA && InitData.SOUNDDATA.soundArray && InitData.SOUNDDATA.soundArray.length > 19)
         {
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[19], 1);
         }
         var isCombat:Boolean = Boolean(this._last && this._last.uiName && this._last.uiName.indexOf("Pause") != -1);
         new UITutorialsModal(_sp, isCombat, this);
      }
      
      override public function afterFadeIn() : void
      {
         this.updateChoose();
         if(ConfigAndStory.HOWTOPLAY[0] == 0)
         {
            new UIHelp(0,false,_sp,Attach.attach("UI_Help") as MovieClip,"UI_Help",_ui,this);
         }
      }
      
      public function updateChoose() : void
      {
         var hiMC:DisplayObject = _mov.getChildByName("$choosehi");
         if(hiMC)
         {
            hiMC.visible = false;
         }
         (_mov.getChildByName("$choosebloom") as MovieClip).gotoAndStop(2 - this.config[0]);
         (_mov.getChildByName("$choosehdr") as MovieClip).gotoAndStop(2 - this.config[1]);
         (_mov.getChildByName("$choosesound") as MovieClip).gotoAndStop(2 - this.config[3]);
         (_mov.getChildByName("$choosemusic") as MovieClip).gotoAndStop(2 - this.config[4]);
         (_mov.getChildByName("$choosebg") as MovieClip).gotoAndStop(2 - this.config[5]);
      }
      
      public function configChange(param1:int) : void
      {
         if(this.config[param1] == 0)
         {
            this.config[param1] = 1;
         }
         else
         {
            this.config[param1] = 0;
         }
         this.updateChoose();
      }
      
      public function changes() : Boolean
      {
         var _loc1_:int = 0;
         while(_loc1_ < this.config.length)
         {
            if(this.config[_loc1_] != ConfigAndStory.CONFIG[_loc1_])
            {
               return true;
            }
            _loc1_++;
         }
         var curMode:int = ConfigAndStory.FULLSCREEN ? 3 : (ConfigAndStory.RESOLUTION == ConfigAndStory.RES_1080 ? 2 : (ConfigAndStory.RESOLUTION == ConfigAndStory.RES_720 ? 1 : 0));
         if(this.selectedDisplayMode != curMode)
         {
            return true;
         }
         return false;
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "choosebloom")
         {
            this.configChange(0);
            if(this.config[1] == 1)
            {
               this.configChange(1);
            }
         }
         else if(_loc2_ == "choosehdr")
         {
            this.configChange(1);
            if(this.config[0] == 1)
            {
               this.configChange(0);
            }
         }
         else if(_loc2_ == "choosehi")
         {
            this.onResClick(null);
         }
         else if(_loc2_ == "choosesound")
         {
            this.configChange(3);
         }
         else if(_loc2_ == "choosemusic")
         {
            this.configChange(4);
         }
         else if(_loc2_ == "choosebg")
         {
            this.configChange(5);
         }
         else if(_loc2_ == "ok")
         {
            if(this.changes())
            {
               new UIReally(this.okFunc,this.cancelFunc,null,"Change Settings?","It\'ll be auto saved.",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
            else
            {
               removeSelf();
            }
         }
         else if(_loc2_ == "cancel")
         {
            removeSelf();
         }
         else if(_loc2_ == "help")
         {
            var isCombat:Boolean = Boolean(this._last && this._last.uiName && this._last.uiName.indexOf("Pause") != -1);
            new UITutorialsModal(_sp, isCombat, this);
         }
      }
      
      public function okFunc() : void
      {
         ConfigAndStory.CONFIG = this.config;
         if(this.selectedDisplayMode == 3)
         {
            ConfigAndStory.setFullscreen(true);
         }
         else
         {
            ConfigAndStory.setFullscreen(false);
            if(this.selectedDisplayMode == 2)
            {
               ConfigAndStory.RESOLUTION = ConfigAndStory.RES_1080;
            }
            else if(this.selectedDisplayMode == 1)
            {
               ConfigAndStory.RESOLUTION = ConfigAndStory.RES_720;
            }
            else
            {
               ConfigAndStory.RESOLUTION = ConfigAndStory.RES_640;
            }
         }
         if(this.config[0] == 0 && this.config[1] == 0)
         {
            ConfigAndStory.QUALITY = ConfigAndStory.QUALITY_LOW;
         }
         else if(this.config[0] == 1 && this.config[1] == 1)
         {
            ConfigAndStory.QUALITY = ConfigAndStory.QUALITY_HIGH;
         }
         else
         {
            ConfigAndStory.QUALITY = ConfigAndStory.QUALITY_MEDIUM;
         }
         ConfigAndStory.makeChange();
         removeSelf();
         MochiEvents.trackEvent("change_option");
      }
      
      public function cancelFunc() : void
      {
      }
   }
}

import f4.data.InitData;
import flash.display.DisplayObjectContainer;
import flash.display.GradientType;
import flash.display.Graphics;
import flash.display.Sprite;
import flash.events.MouseEvent;
import flash.filters.GlowFilter;
import flash.geom.Matrix;
import flash.text.TextField;
import flash.text.TextFormat;
import flash.text.TextFormatAlign;
import ui.base.UIStyles;
import ui.pages.UIOptions;

class UITutorialsModal extends Sprite
{
   private var _parentContainer:DisplayObjectContainer;
   private var _backdrop:Sprite;
   private var _panel:Sprite;
   private var _optionsUI:UIOptions;
   private var _isCombat:Boolean;

   public function UITutorialsModal(parentContainer:DisplayObjectContainer, isCombat:Boolean, optionsUI:UIOptions = null)
   {
      super();
      this._parentContainer = parentContainer;
      this._isCombat = isCombat;
      this._optionsUI = optionsUI;
      this.buildModal();
      this._parentContainer.addChild(this);
   }

   private function buildModal() : void
   {
      this._backdrop = new Sprite();
      var bgG:Graphics = this._backdrop.graphics;
      bgG.beginFill(0x000000, 0.82);
      bgG.drawRect(0, 0, 640, 480);
      bgG.endFill();
      this._backdrop.addEventListener(MouseEvent.CLICK, this.onCloseClick);
      this.addChild(this._backdrop);

      this._panel = new Sprite();
      this._panel.x = 35;
      this._panel.y = 30;
      var pG:Graphics = this._panel.graphics;
      pG.beginFill(0x090F1B, 0.96);
      var mat:Matrix = new Matrix();
      mat.createGradientBox(570, 420, 0, 0, 0);
      pG.lineStyle(1.5);
      pG.lineGradientStyle(GradientType.LINEAR, [0x00CCFF, 0x336699, 0x00CCFF], [0.8, 0.4, 0.8], [0, 128, 255], mat);
      
      pG.moveTo(15, 0);
      pG.lineTo(555, 0);
      pG.lineTo(570, 15);
      pG.lineTo(570, 405);
      pG.lineTo(555, 420);
      pG.lineTo(15, 420);
      pG.lineTo(0, 405);
      pG.lineTo(0, 15);
      pG.lineTo(15, 0);
      pG.endFill();

      var titleTxt:TextField = new TextField();
      titleTxt.width = 570;
      titleTxt.height = 28;
      titleTxt.y = 12;
      titleTxt.selectable = false;
      titleTxt.mouseEnabled = false;
      UIStyles.applyFormat(titleTxt, UIStyles.FONT_TITLE, UIStyles.SIZE_HEADER, UIStyles.COLOR_SYSTEM_LIGHT, true, TextFormatAlign.CENTER);
      UIStyles.applyHoloGlow(titleTxt, UIStyles.COLOR_SYSTEM_CYAN);
      titleTxt.text = this._isCombat ? "SECTION TUTORIALS: COMBAT & FLIGHT" : "SECTION TUTORIALS: BASE & HANGAR";
      this._panel.addChild(titleTxt);

      var divG:Graphics = this._panel.graphics;
      divG.lineStyle(1, 0x335577, 0.6);
      divG.moveTo(20, 42);
      divG.lineTo(550, 42);

      var subtitleTxt:TextField = new TextField();
      subtitleTxt.width = 570;
      subtitleTxt.height = 18;
      subtitleTxt.y = 44;
      subtitleTxt.selectable = false;
      subtitleTxt.mouseEnabled = false;
      UIStyles.applyFormat(subtitleTxt, UIStyles.FONT_BODY, UIStyles.SIZE_HUD, 0x88BBDD, false, TextFormatAlign.CENTER);
      subtitleTxt.text = this._isCombat 
         ? "Guidance and tactical protocols for active battlefield engagements" 
         : "Operational procedures for ship assembly, fleet deployment, and logistics";
      this._panel.addChild(subtitleTxt);

      if(this._isCombat)
      {
         this.createCard(20, 68, 260, 138, "1. FLIGHT & WEAPONS CONVERGENCE",
            "• WASD / Arrows: Steer, reverse, and forward thrust.\n" +
            "• Mouse Pointer: Dynamic aim & ballistic flight vector.\n" +
            "• Space: Afterburner boost (consumes energy).\n" +
            "• [Q] Key / HUD Pill: Toggle weapon autofire mode.\n" +
            "• [C] Key / HUD Pill: Toggle camera distance (Close/Wide).");

         this.createCard(290, 68, 260, 138, "2. TACTICAL AI & SQUAD FORMATIONS",
            "• Wingmen execute pre-battle assigned tactical roles.\n" +
            "• Roles: ESCORT, HUNT, SHIELD, ANCHOR, EVADE, SUPPORT.\n" +
            "• Formation Leader: Michael or Captain Tom.\n" +
            "• [E] Key / HUD Mode Pill: Cycles Captain tactical modes:\n" +
            "  - Stand Ground (defensive firing)\n" +
            "  - Hunt Enemies (aggressive pursuit)\n" +
            "  - Protect Flagship (all wingmen rally around leader)");

         this.createCard(20, 214, 530, 140, "3. FLAGSHIP PROTECTION & TELEMETRY",
            "• Flagship Defense: Captain Tom commands his own battle cruiser in priority missions.\n" +
            "• Mission Critical: If Captain Tom\'s flagship falls below 0% HP, the mission fails immediately!\n" +
            "• Tactical HUD: Real-time telemetry monitors the Captain\'s hull integrity with tiered warnings:\n" +
            "  - 70% Caution (Green) | 50% Alert (Yellow) | 30% Critical (Orange) | 10% Emergency (Red)\n" +
            "• Repair & Support: Equipping escort craft with repair beams or shield roles provides automated healing.\n" +
            "• Threat Prioritization: AI escorts actively prioritize intercepting incoming projectiles directed at Tom.");
      }
      else
      {
         this.createCard(20, 68, 260, 138, "1. SHIPBUILDER & ASSEMBLY",
            "• Hardpoints: Connect hulls, wings, engines, and guns.\n" +
            "• Touch & Mouse: Tap part to select (cyan glow); drag to move.\n" +
            "• Magnetic Snapping: Green snap preview and visible socket.\n" +
            "• Cyber Controls: [ROTATE] 90°, [FLIP] wings, [REMOVE] to inventory, [CENTER] canvas view.\n" +
            "• Canvas Pan: Drag empty background without part conflict.");

         this.createCard(290, 68, 260, 138, "2. AI COMPANION DEPLOYMENT",
            "• Unlocks at Story Mission 14.\n" +
            "• Assign custom-built craft to companion wingmen slots.\n" +
            "• Configure pre-battle tactical behavior roles:\n" +
            "  ESCORT, HUNT, SHIELD, ANCHOR, EVADE, SUPPORT.\n" +
            "• Assign squad leader to Michael or Captain Tom.\n" +
            "• Deployed fleet launches alongside you in combat.");

         this.createCard(20, 214, 260, 140, "3. SUPPLY SHOP & PART TELEMETRY",
            "• Purchase new ship hulls, wings, and weapon systems.\n" +
            "• Column 6 (POW/RNG): Displays exact effective firing range calculated from muzzle velocity and projectile lifespan.\n" +
            "• Inspect weapon stats, turn speed, and hull capacity before purchase.");

         this.createCard(290, 214, 260, 140, "4. HANGAR REPAIR & FLEET READINESS",
            "• Restores damaged hull plating and destroyed modules post-battle.\n" +
            "• Individual Part Repair: Select parts to repair selectively or click \'Fix All\' for full overhaul.\n" +
            "• Keep fleet fully repaired before launching high-threat sector sorties.");
      }

      var btnClose:Sprite = this.createButton("CLOSE", 235, 368, 100, 26, this.onCloseClick);
      this._panel.addChild(btnClose);

      this.addChild(this._panel);
   }

   private function createCard(x:Number, y:Number, w:Number, h:Number, title:String, text:String) : void
   {
      var card:Sprite = new Sprite();
      card.x = x;
      card.y = y;
      var g:Graphics = card.graphics;
      g.beginFill(0x101826, 0.9);
      g.lineStyle(1, 0x1E3550, 0.8);
      g.drawRoundRect(0, 0, w, h, 6, 6);
      g.endFill();

      var tTitle:TextField = new TextField();
      tTitle.x = 8;
      tTitle.y = 6;
      tTitle.width = w - 16;
      tTitle.height = 20;
      tTitle.selectable = false;
      tTitle.mouseEnabled = false;
      UIStyles.applyFormat(tTitle, UIStyles.FONT_TITLE, UIStyles.SIZE_HUD, 0x00E5FF, true, TextFormatAlign.LEFT);
      tTitle.filters = [new GlowFilter(0x0088CC, 0.7, 3, 3, 2, 1)];
      tTitle.text = title;
      card.addChild(tTitle);

      var tBody:TextField = new TextField();
      tBody.x = 8;
      tBody.y = 26;
      tBody.width = w - 16;
      tBody.height = h - 30;
      tBody.multiline = true;
      tBody.wordWrap = true;
      tBody.selectable = false;
      tBody.mouseEnabled = false;
      UIStyles.applyFormat(tBody, UIStyles.FONT_BODY, UIStyles.SIZE_HUD - 1, 0xD0E5F5, false, TextFormatAlign.LEFT);
      tBody.text = text;
      card.addChild(tBody);

      this._panel.addChild(card);
   }

   private function createButton(label:String, x:Number, y:Number, w:Number, h:Number, onClick:Function) : Sprite
   {
      var btn:Sprite = new Sprite();
      btn.x = x;
      btn.y = y;
      btn.buttonMode = true;
      btn.useHandCursor = true;
      btn.mouseChildren = false;

      var drawBtn:Function = function(hover:Boolean, down:Boolean):void
      {
         btn.graphics.clear();
         var bg:uint = down ? 0x0A1824 : (hover ? 0x183854 : 0x0D2235);
         var border:uint = hover ? 0x00CCFF : 0x285577;
         btn.graphics.lineStyle(1.2, border, 0.9);
         btn.graphics.beginFill(bg, 0.95);
         btn.graphics.drawRoundRect(0, 0, w, h, 4, 4);
         btn.graphics.endFill();
      };
      drawBtn(false, false);

      var txt:TextField = new TextField();
      var tf:TextFormat = new TextFormat("Arial", 11, 0x88DDFF, true);
      tf.align = TextFormatAlign.CENTER;
      txt.defaultTextFormat = tf;
      txt.text = label;
      txt.width = w;
      txt.height = h;
      txt.y = 4;
      txt.selectable = false;
      txt.mouseEnabled = false;
      btn.addChild(txt);

      btn.addEventListener(MouseEvent.MOUSE_OVER, function(e:MouseEvent):void { drawBtn(true, false); txt.textColor = 0xFFFFFF; });
      btn.addEventListener(MouseEvent.MOUSE_OUT, function(e:MouseEvent):void { drawBtn(false, false); txt.textColor = 0x88DDFF; });
      btn.addEventListener(MouseEvent.MOUSE_DOWN, function(e:MouseEvent):void { drawBtn(true, true); });
      btn.addEventListener(MouseEvent.MOUSE_UP, function(e:MouseEvent):void { drawBtn(true, false); });
      btn.addEventListener(MouseEvent.CLICK, onClick);
      return btn;
   }

   private function onCloseClick(e:MouseEvent) : void
   {
      if(this.parent)
      {
         this.parent.removeChild(this);
      }
      if(InitData.SOUNDC2 && InitData.SOUNDDATA && InitData.SOUNDDATA.soundArray)
      {
         InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[16]);
      }
   }
}

