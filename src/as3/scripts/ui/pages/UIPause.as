package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import fullshit.tool.Attach;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.make.MakeUI;
   
   public class UIPause extends UIBase
   {
      
      public function UIPause(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         super(param1,param2,param3,param4,param5);
         if(ConfigAndStory.CONFIG[4] == 1)
         {
            InitData.SOUNDC2.allTo(0.2,-0.05,false);
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "option")
         {
            new UIOptions(_sp,Attach.attach("UI_Options") as MovieClip,"UI_Options",_ui,this);
         }
         else if(_loc2_ == "mission")
         {
            new UIMission(_sp,Attach.attach("UI_Mission") as MovieClip,"UI_Mission",_ui,this);
         }
         else if(_loc2_ == "restart")
         {
            new UIReally(this.restartYes,this.restartNo,null,"Retry?","Restart the battle?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
         else if(_loc2_ == "resume")
         {
            InitData.CORE.pause = false;
            removeSelf();
            InitData.SOUNDC2.allResume();
         }
      }
      
      public function restartYes() : void
      {
         InitData.CORE.repair.deadFalse();
         this.endGame();
         removeAll();
         MakeUI.makeChoose(this,_sp,_ui);
         ConfigAndStory.saveData();
         ConfigAndStory.save();
      }
      
      public function restartNo() : void
      {
      }
      
      public function endGame() : void
      {
         InitData.CORE.winAct(false);
      }
   }
}

