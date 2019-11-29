# Log
Utility class that enables quickly and easily logging variable values to the console in Unity.

## Usage Example 1

```C#
float floatVariable = 3f;

Log.Value(()=>floatVariable);

// console output:
// "floatVariable=3"
```

## Usage Example 2

```C#
bool boolVariable = true;
Vector2 vector2Variable = new Vector2(1f,2f);

Log.Values(()=>boolVariable, ()=>vector2Variable);

// console output:
// "boolVariable=true, vector2Variable=(1, 2)"
```

## Usage Example 3

```C#
class Example
{
	public static float StaticFloatField = 1f;

	public Object objectField;

	private int privateIntField = 100;

	public int IntProperty
	{
		get
		{
			return intField * 2;
		}
	}

	public void LogState()
	{
		Log.State(this);

		// console output:
		// "Example state: objectField=null, IntProperty=200"


		Log.State(this, true, true);

		// console output:
		// "Example state: StaticFloatField=1f, objectField=null, privateIntField=100, IntProperty=200"
	}
}
```

## Usage Example 4

```C#
Log.State(typeof(UnityEngine.Time));

/* console output:
Time state: 
time=67.29188
timeSinceLevelLoad=67.29188
deltaTime=0.3333333
fixedTime=67.28
unscaledTime=12669.92
fixedUnscaledTime=12669.91
unscaledDeltaTime=167.1589
fixedUnscaledDeltaTime=0.02
fixedDeltaTime=0.02
maximumDeltaTime=0.3333333
smoothDeltaTime=0.3324916
maximumParticleDeltaTime=0.03
timeScale=1
frameCount=300
renderedFrameCount=300
realtimeSinceStartup=12672.38
captureDeltaTime=0
captureFramerate=0
inFixedTimeStep=False
*/
```