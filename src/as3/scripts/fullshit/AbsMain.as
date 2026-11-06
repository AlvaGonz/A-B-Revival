package fullshit
{
   import flash.display.MovieClip;
   import flash.display.Stage;
   import flash.ui.ContextMenu;
   import fullshit.ctrl.KeyCtrl;
   import fullshit.ctrl.MouseCtrl;
   
   public class AbsMain extends MovieClip
   {
      
      public static var keyCtrl:KeyCtrl;
      
      public static var mouseCtrl:MouseCtrl;
      
      public static var fps:Number;
      
      public static var STAGE:Stage;
      
      public static var w:int = 640;
      
      public static var h:int = 480;
      
      public static const SCALE:Number = 1;
      
      public function AbsMain()
      {
         super();
      }
      
      public function initSup() : void
      {
         keyCtrl = new KeyCtrl(stage);
         mouseCtrl = new MouseCtrl(stage);
         w = 640;
         h = 480;
         fps = stage.frameRate;
         STAGE = stage;
         this.hideright();
      }
      
      private function hideright() : void
      {
         var _loc1_:ContextMenu = new ContextMenu();
         _loc1_.hideBuiltInItems();
         this.contextMenu = _loc1_;
      }
   }
}

