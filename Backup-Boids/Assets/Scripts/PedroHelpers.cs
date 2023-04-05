using UnityEngine;
//using XInputDotNetPure; // Package téléchargable sur https://github.com/speps/XInputDotNet/releases

public static class PedroHelpers
{
    #region Raycast2D
    public static RaycastHit2D ShootRay(Vector2 from, Vector2 dir, float distance, int layerMask, Color debugColor)
    {
        Debug.DrawRay(from, dir * distance, debugColor);
        return Physics2D.Raycast(from, dir, distance, layerMask);
    }
    
    public static RaycastHit2D ShootRay(Vector2 from, Vector2 dir, float distance, Color debugColor)
    {
        Debug.DrawRay(from, dir * distance, debugColor);
        return Physics2D.Raycast(from, dir, distance);
    }
    #endregion

    #region Random 2D Position
    public static Vector2 GenerateRandomPosIn2DArea(Vector2 area)
    {
        return new Vector2(
            Random.Range(-area.x,area.x), 
            Random.Range(-area.y,area.y));
    } 
    
    public static Vector2 GenerateRandomPosIn2DArea(float height, float width) 
    {
        return new Vector2(
            Random.Range(-width, width),
            Random.Range(-height, height));
    }
    
    public static Vector2 GenerateRandomPosIn2DArea(float squareHeightAndWidth) 
    {
        return new Vector2(
            Random.Range(-squareHeightAndWidth, squareHeightAndWidth), 
            Random.Range(-squareHeightAndWidth, squareHeightAndWidth));
    }
    #endregion
    
    #region Random 3D Position
    public static Vector3 GenerateRandomPosInCube(float cubeSize)
    {
        return new Vector3(
            Random.Range(-cubeSize /2 ,cubeSize /2), 
            Random.Range(-cubeSize /2,cubeSize /2),
            Random.Range(-cubeSize /2,cubeSize /2));
    }
    
    public static Vector3 GenerateRandomPosInCube(Vector3 center, float cubeSize)
    {
        return new Vector3(
            Random.Range(center.x + -cubeSize /2, center.x + cubeSize /2), 
            Random.Range(center.y + -cubeSize /2, center.y + cubeSize /2),
            Random.Range(center.z + -cubeSize /2, center.z + cubeSize /2));
    }
    
    public static Vector3 GenerateRandomPosIn3DArea(Vector3 area)
    {
        return new Vector3(
            Random.Range(-area.x,area.x), 
            Random.Range(-area.y,area.y),
            Random.Range(-area.z,area.z));
    }
    #endregion

    #region Shuffle List or Array
    public static void ShuffleTransformArray(Transform[] t)
    {
        for (int i = 0; i < t.Length; i++ )
        {
            Transform tmp = t[i];
            int r = Random.Range(i, t.Length);
            t[i] = t[r];
            t[r] = tmp;
        }
    }
    #endregion

    #region RandomColor
    public static Color RandomColor()
    {
        return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }
    #endregion
}

/*
public class VibrationManager : MonoBehaviour
{
    private static PlayerIndex _playerIndex;

    public static IEnumerator GenerateGamePadVibration(float duration = 1f, float leftMotorForce = 0.1f, float rightMotorForce = 0.1f)
    {
        GamePad.SetVibration(_playerIndex, leftMotorForce, rightMotorForce);
        yield return new WaitForSeconds(duration);
        GamePad.SetVibration(_playerIndex, 0f, 0f);
    }

    public static void StopVibrating() => GamePad.SetVibration(_playerIndex, 0f, 0f);
}
*/
