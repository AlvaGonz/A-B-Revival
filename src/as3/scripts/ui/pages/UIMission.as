package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIMission extends UIBase
   {
      
      public var endFunc:Function;
      
      public function UIMission(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null, param6:Function = null)
      {
         (param2.getChildByName("txtWords") as TextField).text = ConfigAndStory.MISSIONS.missionWords[ConfigAndStory.STORY];
         (param2.getChildByName("txtTitle") as TextField).text = "BATTLE-" + (ConfigAndStory.STORY + 1);
         this.endFunc = param6;
         super(param1,param2,param3,param4,param5);
         if(ConfigAndStory.CONFIG[4] == 1)
         {
            InitData.SOUNDC2.allTo(0.2,-0.05,false);
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "ok")
         {
            if(this.endFunc != null)
            {
               this.endFunc();
            }
            InitData.SOUNDC2.allResume();
            removeSelf();
         }
      }
   }
}

