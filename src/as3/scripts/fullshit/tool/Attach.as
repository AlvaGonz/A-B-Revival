package fullshit.tool
{
   import flash.utils.getDefinitionByName;
   import ui.UICtrl;
   
   public final class Attach
   {
      
      public function Attach()
      {
         super();
      }
      
      public static function attach(param1:String) : Object
      {
         var tclass:Class = null;
         var iname:String = param1;
         try
         {
            tclass = getDefinitionByName(iname) as Class;
         }
         catch(e:Error)
         {
            try
            {
               tclass = Fly4UI.DOMAIN.getDefinition(iname) as Class;
            }
            catch(e:Error)
            {
               tclass = UICtrl.DOMAIN.getDefinition(iname) as Class;
            }
         }
         return new tclass();
      }
   }
}

