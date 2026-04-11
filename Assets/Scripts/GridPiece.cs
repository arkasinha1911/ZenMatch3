using System.Collections;
using UnityEngine;

public enum PowerUpType
{
    None,
    HorizontalBomb,
    VerticalBomb,
    ColorBomb
}

public class GridPiece : MonoBehaviour
{
    public int x;
    public int y;
    public int pieceType;
    public PowerUpType powerUp = PowerUpType.None;

    public bool isMoving = false;

    public void SetCoordinates(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public void MoveToPosition(Vector3 targetPosition, float duration = 0.2f)
    {
        StartCoroutine(MoveCoroutine(targetPosition, duration));
    }

    private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration)
    {
        isMoving = true;
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }
}
