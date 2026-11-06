package ui.pages
{
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.net.FileReference;
   import fullshit.tool.Attach;
   import fullshit.tool.SaveLoad;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIData extends UIBase
   {
      
      public var sl:SaveLoad;
      
      public function UIData(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         super(param1,param2,param3,param4,param5);
         this.sl = new SaveLoad("anb",this.loaded);
      }
      
      private function loaded(param1:Event) : void
      {
         var _loc2_:Boolean = ConfigAndStory.readOutData((param1.target as FileReference).data);
         if(_loc2_)
         {
            removeSelf();
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "back")
         {
            removeSelf();
         }
         else if(_loc2_ == "clean")
         {
            new UIReally(this.cleanYes,null,null,"Reset Data?","Wanna return to ZERO?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
         else if(_loc2_ == "save")
         {
            this.saveData();
         }
         else if(_loc2_ == "load")
         {
            new UIReally(this.loadYes,null,null,"Load Data?","Your data will be overwrited.",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
      }
      
      public function saveData() : void
      {
         this.sl.saveAll(ConfigAndStory.makeOutData(),"AnBSave");
         MochiEvents.trackEvent("save_data");
      }
      
      public function cleanYes() : void
      {
         ConfigAndStory.cleanData();
         MochiEvents.trackEvent("clean_data");
      }
      
      public function loadYes() : void
      {
         this.sl.load();
         MochiEvents.trackEvent("load_data");
      }
   }
}

