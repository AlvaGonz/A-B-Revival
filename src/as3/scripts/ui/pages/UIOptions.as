package ui.pages
{
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIOptions extends UIBase
   {
      
      public var config:Array = ConfigAndStory.CONFIG.concat();
      
      public function UIOptions(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         _mov = param2;
         this.updateChoose();
         super(param1,param2,param3,param4,param5);
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
         (_mov.getChildByName("$choosebloom") as MovieClip).gotoAndStop(2 - this.config[0]);
         (_mov.getChildByName("$choosehdr") as MovieClip).gotoAndStop(2 - this.config[1]);
         (_mov.getChildByName("$choosehi") as MovieClip).gotoAndStop(2 - this.config[2]);
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
            this.configChange(2);
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
         if(this.config[2] == 1)
         {
            ConfigAndStory.RESOLUTION = ConfigAndStory.RES_1080;
         }
         else
         {
            ConfigAndStory.RESOLUTION = ConfigAndStory.RES_720;
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

