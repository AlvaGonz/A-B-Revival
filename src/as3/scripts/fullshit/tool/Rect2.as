package fullshit.tool
{
   import flash.geom.Rectangle;
   
   public class Rect2
   {
      
      public function Rect2()
      {
         super();
      }
      
      public function rectIntersects(param1:Rectangle, param2:Rectangle) : Boolean
      {
         if(param1.x < param2.x)
         {
            if(param1.x + param1.width < param2.x)
            {
               return false;
            }
         }
         else if(param1.x > param2.x + param2.width)
         {
            return false;
         }
         if(param1.y < param2.y)
         {
            if(param1.y + param1.height < param2.y)
            {
               return false;
            }
         }
         else if(param1.y > param2.y + param2.height)
         {
            return false;
         }
         return true;
      }
      
      public function xyInRect(param1:Number, param2:Number, param3:Rectangle) : Boolean
      {
         return !(param1 < param3.x || param1 > param3.x + param3.width || param2 < param3.y || param2 > param3.y + param3.height);
      }
   }
}

