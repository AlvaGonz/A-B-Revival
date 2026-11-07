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
   
   public class UIOptions extends UIBase
   {
      
      public var config:Array = ConfigAndStory.CONFIG.concat();
      
      public var selectedDisplayMode:int = 0;
      
      private var btnRes:Sprite;
      
      private var txtRes:TextField;
      
      private var isResHovered:Boolean = false;
      
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
            new UIHelp(0,false,_sp,Attach.attach("UI_Help") as MovieClip,"UI_Help",_ui,this);
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

