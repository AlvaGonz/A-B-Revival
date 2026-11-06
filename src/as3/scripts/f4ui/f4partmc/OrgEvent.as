package f4ui.f4partmc
{
   import f4data.F4DataOrg;
   import flash.events.Event;
   
   public class OrgEvent extends Event
   {
      
      public var org:F4DataOrg;
      
      public function OrgEvent(param1:String, param2:F4DataOrg, param3:Boolean = false, param4:Boolean = false)
      {
         super(param1,param3,param4);
         this.org = param2;
      }
   }
}

