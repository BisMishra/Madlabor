public class listedPath
{
    public int pathIndex { get; set; }
    public int index { get; set; }
    public float xCoord { get; set; }
    public float yCoord { get; set; }
    public float zCoord { get; set; }
    public listedPath(int pathIndex, int index, float xCoord, float yCoord, float zCoord){
    this.pathIndex = pathIndex;
    this.index = index;
    this.xCoord = xCoord;
    this.yCoord = yCoord;
    this.zCoord = zCoord;
    }

    //I love that the tools are available but the documentation is really abysmal, why was I not told that I need a parameter-less constructor? Why did ChatGPT tell me this?
    public listedPath(){}
}
