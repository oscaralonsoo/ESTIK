using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))] // Volvemos al Rigidbody 3D
public class LanzamientoConstreñido : MonoBehaviour
{
    private Vector2 posicionInicial;
    private Vector2 posicionFinal;
    private Rigidbody rb;

    [Tooltip("Ajusta esto para que el lanzamiento sea más fuerte o más flojo")]
    public float multiplicadorFuerza = 0.05f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Al hacer clic
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            posicionInicial = Mouse.current.position.ReadValue();
        }

        // Al soltar el clic
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            posicionFinal = Mouse.current.position.ReadValue();
            Lanzar();
        }
    }

    void Lanzar()
    {
        // 1. Calculamos el vector exacto de tu deslizamiento en pantalla
        Vector2 direccion = posicionFinal - posicionInicial;

        // 2. Si quieres que funcione como Angry Birds (estiras hacia atrás para lanzar hacia adelante),
        // quita las dos barras (//) de la siguiente línea:
        // direccion = -direccion;

        // 3. Convertimos esa dirección 2D a un vector 3D, pero dejando la Z en 0
        Vector3 fuerzaLanzamiento = new Vector3(direccion.x, direccion.y, 0f);

        // 4. Aplicamos la fuerza al Rigidbody 3D
        rb.AddForce(fuerzaLanzamiento * multiplicadorFuerza, ForceMode.Impulse);
    }
}