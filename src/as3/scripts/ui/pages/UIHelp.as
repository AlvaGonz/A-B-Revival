package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIHelp extends UIBase
   {
      
      public var ingame:Boolean;
      
      public function UIHelp(param1:int, param2:Boolean, param3:DisplayObjectContainer, param4:MovieClip, param5:String, param6:UICtrl, param7:UIBase = null)
      {
         param4.gotoAndStop(param1 + 1);
         this.ingame = param2;
         if(ConfigAndStory.HOWTOPLAY[param1] == 0)
         {
            ConfigAndStory.HOWTOPLAY[param1] = 1;
            ConfigAndStory.save();
         }
         super(param3,param4,param5,param6,param7);
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "back")
         {
            if(this.ingame)
            {
               InitData.CORE.pause = false;
            }
            removeSelf();
         }
      }
   }
}

