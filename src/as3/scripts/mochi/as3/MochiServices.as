package mochi.as3
{
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   
   public class MochiServices
   {
      public static const CONNECTED:String = "onConnected";
      
      public static var onError:Object;
      
      public static var netup:Boolean = false;
      
      public static var netupAttempted:Boolean = true;
      
      public static var widget:Boolean = false;
      
      public function MochiServices()
      {
         super();
      }
      
      public static function get id() : String
      {
         return "";
      }
      
      public static function get clip() : Object
      {
         return null;
      }
      
      public static function get childClip() : Object
      {
         return null;
      }
      
      public static function getVersion() : String
      {
         return "3.0.4";
      }
      
      public static function allowDomains(param1:String) : String
      {
         return param1;
      }
      
      public static function isNetworkAvailable() : Boolean
      {
         return false;
      }
      
      public static function set comChannelName(param1:String) : void
      {
      }
      
      public static function get connected() : Boolean
      {
         return false;
      }
      
      public static function warnID(param1:String, param2:Boolean) : void
      {
      }
      
      public static function connect(param1:String, param2:Object, param3:Object = null) : void
      {
      }
      
      public static function disconnect() : void
      {
      }
      
      public static function stayOnTop() : void
      {
      }
      
      public static function doClose() : void
      {
      }
      
      public static function bringToTop(param1:Event = null) : void
      {
      }
      
      public static function setContainer(param1:Object = null, param2:Boolean = true) : void
      {
      }
      
      public static function send(param1:String, param2:Object = null, param3:Object = null, param4:Object = null) : void
      {
      }
      
      public static function addLinkEvent(param1:String, param2:String, param3:DisplayObjectContainer, param4:Function = null) : void
      {
      }
      
      public static function addEventListener(param1:String, param2:Function) : void
      {
      }
      
      public static function triggerEvent(param1:String, param2:Object) : void
      {
      }
      
      public static function removeEventListener(param1:String, param2:Function) : void
      {
      }
   }
}
