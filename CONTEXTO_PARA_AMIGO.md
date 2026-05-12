# PingPongMania — Contexto y código para arreglar la física de la pelota

## ¿Qué es el juego?
Juego de **ping pong en VR** (Meta Quest / OVR SDK) hecho en **Unity** (C#). El jugador
sostiene una raqueta con la mano derecha (`RightHandAnchor`), saca con la mano
izquierda (`LeftHandAnchor`), y juega contra un **jefe estático** que devuelve la pelota.

Mecánica resumida:
- El jefe es un trigger estático. Cuando la pelota entra a su zona, congela 0.1 s y la lanza con un vector fijo (con leve variación en X) hacia el jugador.
- El jugador golpea la pelota con la raqueta — reflexión + impulso del swing.
- La mesa está dividida en dos mitades (`TableBounce`) que registran en qué lado rebotó.
- Un watchdog mata la pelota si se sale del mundo o queda quieta demasiado tiempo.

## EL PROBLEMA (lo importante)
Las **físicas de la pelota son inconsistentes**:
- A veces rebota normal contra la mesa o la raqueta.
- A veces NO rebota (se queda muerta o atraviesa).
- A veces sale disparada como si nada (velocidad enorme, irreal).

**Objetivo:**
1. Diseñar **una pelota nueva, desde 0, sencilla y predecible**.
2. **Simplificar** los scripts que la usan (raqueta y boss principalmente).
3. Nada de gravedad manual rara, nada de efectos de color (eso se quita).
4. Que sea **divertida**: golpe siempre rebota, velocidad acotada, arco bonito.

## Setup actual (Unity)
- Prefab: `PingPongBall Variant.prefab`
  - Rigidbody, Tag = `Ball`, Layer = 4 (Water — sí, raro, pero lo usan así).
  - CollisionDetection = Continuous (m_CollisionDetection: 1).
- Physic Material `Rebote_PingPong`: Bounciness = 1, Friction = 0, BounceCombine = Maximum.
- El boss usa OnTriggerEnter para detectar la pelota (no colisión).
- La mesa usa OnCollisionEnter (no trigger) para registrar rebotes.

## Lo que NO queremos
- Gravedad manual con `AddForce(Vector3.down...)` — causa inconsistencias.
- Cambiar `useGravity` / `isKinematic` 5 veces en distintos scripts — eso rompe el estado.
- Efectos de color azul/naranja modificando gravedad — todo eso se elimina.

---

## CÓDIGO ACTUAL (referencia, esto es lo que hay que reemplazar / simplificar)

> Está en **C# de Unity**. Tu amigo puede mandarlo de vuelta en **Java pseudo-Unity**
> (clases con `Awake/Update/FixedUpdate`, `Rigidbody`, `Vector3`, `Collision`) y yo
> lo traduzco. No necesita pensar en sintaxis exacta de C#, solo en la lógica física.

### 1. PelotaBehaviour.cs — script en el prefab de la pelota (el más problemático)
```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PelotaBehaviour : MonoBehaviour
{
    public float gravedadNormal  =  9.81f;
    public float gravedadAzul    =  2.5f;   // <-- quitar (efecto liviano)
    public float gravedadNaranja = 22f;     // <-- quitar (efecto pesado)

    private Rigidbody rb;
    private Renderer  rend;
    private Color     colorOriginal;
    private bool      flotando          = false;
    private bool      efectoColorActivo = false;
    private float     gravedadActual    = 9.81f;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        if (rend != null) colorOriginal = rend.material.color;
        gravedadActual = gravedadNormal;
    }

    // Gravedad manual cuando hay efecto -- PROBLEMÁTICO
    void FixedUpdate() {
        if (efectoColorActivo && rb != null)
            rb.AddForce(Vector3.down * gravedadActual, ForceMode.Acceleration);
    }

    void Update() {
        // Detecta agarre por OVRGrabbable o por velocidad > 0.3 -- también frágil
        if (!flotando) return;
        bool agarrada = false;
        OVRGrabbable ovr = GetComponent<OVRGrabbable>();
        if (ovr != null && ovr.isGrabbed) agarrada = true;
        if (!agarrada && rb != null && rb.linearVelocity.magnitude > 0.3f) agarrada = true;
        if (agarrada) { rb.useGravity = true; rb.isKinematic = false; flotando = false; }
    }

    public void IniciarFlotando() {
        flotando = true;
        rb.useGravity = false; rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
    }
    public void SetEfectoColor(bool esAzul) { /* CAMBIA GRAVEDAD MANUALMENTE -- QUITAR */ }
    public void ResetarEfectoColor() { /* QUITAR */ }
}
```

### 2. RaquetaJugador.cs — raqueta kinemática siguiendo la mano derecha
```csharp
[RequireComponent(typeof(Rigidbody))]
public class RaquetaJugador : MonoBehaviour
{
    public Transform manoDerecha;
    public Vector3   offsetPosicion = Vector3.zero;
    public Vector3   offsetRotacion = Vector3.zero;
    public float     fuerzaMinima       = 3.5f;
    public float     velocidadMax       = 11f;
    public float     coefRebote         = 0.75f;
    public float     multiplicadorGolpe = 1.0f;

    private Rigidbody rb;
    private Vector3   velRaqueta;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void FixedUpdate() {
        if (manoDerecha == null) return;
        Quaternion offRot = Quaternion.Euler(offsetRotacion);
        Vector3 nuevaPos  = manoDerecha.position + manoDerecha.rotation * offsetPosicion;
        Quaternion nuevaRot = manoDerecha.rotation * offRot;

        float dt = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        velRaqueta = (nuevaPos - rb.position) / dt;   // velocidad del swing
        rb.MovePosition(nuevaPos);
        rb.MoveRotation(nuevaRot);
    }

    void OnCollisionEnter(Collision col) {
        if (!col.gameObject.CompareTag("Ball")) return;
        Rigidbody rbPelota = col.rigidbody;
        if (rbPelota == null) return;

        Vector3 normal     = col.contacts[0].normal;
        Vector3 velPelota  = rbPelota.linearVelocity;
        Vector3 reflejada  = Vector3.Reflect(velPelota, normal) * coefRebote;
        float   compRaq    = Mathf.Max(0f, Vector3.Dot(velRaqueta, normal));
        Vector3 impulso    = normal * compRaq * multiplicadorGolpe;
        Vector3 vFinal     = reflejada + impulso;

        float compNormal = Vector3.Dot(vFinal, normal);
        if (compNormal < fuerzaMinima) vFinal += normal * (fuerzaMinima - compNormal);
        if (vFinal.magnitude > velocidadMax) vFinal = vFinal.normalized * velocidadMax;

        rbPelota.linearVelocity  = vFinal;
        rbPelota.angularVelocity = Vector3.zero;
    }
}
```

### 3. BossAI.cs — congela la pelota 0.1s y la lanza con vector fijo
```csharp
public class BossAI : MonoBehaviour
{
    public float velocidadZ  = -2.5f;   // hacia el jugador
    public float velocidadY  =  5.0f;   // arco
    public float variacionX  =  0.25f;
    public float delaySaque  =  1.5f;
    private bool ocupado = false;

    void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Ball") || ocupado) return;
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;
        ocupado = true;
        StartCoroutine(LanzarPelota(rb));
    }

    IEnumerator LanzarPelota(Rigidbody rb) {
        rb.isKinematic = true; rb.useGravity = false;
        yield return new WaitForSeconds(0.1f);
        Vector3 v = new Vector3(Random.Range(-variacionX, variacionX), velocidadY, velocidadZ);
        rb.isKinematic = false; rb.useGravity = true;
        rb.linearVelocity = v; rb.angularVelocity = Vector3.zero;
        yield return new WaitForSeconds(1f);
        ocupado = false;
    }
}
```

### 4. TableBounce.cs — detecta en qué mitad rebotó
```csharp
public class TableBounce : MonoBehaviour {
    public bool esMitadJefe = false;
    void OnCollisionEnter(Collision col) {
        if (!col.gameObject.CompareTag("Ball")) return;
        GameManager.instance?.RegistrarRebote(esMitadJefe);
    }
}
```

### 5. SistemaDeServicio.cs — saque del jugador (resumen)
- Pelota se crea en `LeftHandAnchor` con `isKinematic=true`.
- Cada `FixedUpdate` se mueve con `MovePosition` siguiendo la mano.
- Al soltar el trigger izquierdo: `isKinematic=false`, `useGravity=true`,
  `rb.linearVelocity = velocidadMano * multiplicador`.
- Si la mano no se movió: velocidad mínima vertical de 1.5.

### 6. Physic Material y prefab
- `Bounciness = 1`, `Friction = 0`, `BounceCombine = Maximum`.
- Pelota: Rigidbody, CollisionDetection = Continuous, Tag = Ball.

---

## Lo que necesito de vos (amigo)

Reescribime en **Java / pseudo-Unity** (clases con `Awake / FixedUpdate / Update /
OnCollisionEnter`, `Rigidbody`, `Vector3`):

1. **Una pelota nueva minimalista** que:
   - Use `useGravity = true` SIEMPRE que esté en juego (sin gravedad manual).
   - Tenga masa ~0.0027 kg (pelota de ping pong real, opcional).
   - Drag bajo (0.02-0.05) y angularDrag bajo (0.05).
   - CollisionDetection = ContinuousDynamic.
   - PhysicMaterial con bounciness ~0.85 (no 1, eso es perpetuum mobile), friction ~0.1, BounceCombine = Maximum.
   - **Reglas claras** al cambiar de estado: solo el script que toma control de la pelota toca `isKinematic`; al soltarla, lo deja en false y `useGravity = true`.

2. **Raqueta simplificada** que SIEMPRE genere un rebote coherente:
   - Velocidad mínima 4, máxima 10.
   - El swing aporta como mucho un % de la velocidad final (que no se dispare a 50 m/s).
   - Cuando golpea, **siempre** garantiza que la pelota se aleja con componente normal mínima.
   - Sin clamping raro de angularVelocity = 0 si eso jode el efecto (ver qué da mejor).

3. **Boss**: dejarlo casi igual pero con valores que aseguren que la pelota pase la red (probablemente velY ≈ 4-5, velZ ≈ -3).

4. **Quitar todo lo de efectos de color / gravedad manual** del PelotaBehaviour.

5. **Recomendar valores concretos** del PhysicMaterial y del Rigidbody para que el rebote contra la mesa sea siempre el mismo (que no salte 2 m una vez y 10 cm la siguiente).

Si querés, podés escribir directamente las **3 clases (Pelota, Raqueta, Boss) en
Java estilo Unity** y un comentario al final con los valores del prefab/material.
Claude se encarga de traducirlo a C# tal cual.

### Pista del diagnóstico (lo que sospecho)
- El bug viene de mezclar gravedad real + gravedad manual (`AddForce`) en `PelotaBehaviour.FixedUpdate`. A veces el código deja `useGravity` mal sincronizado con `efectoColorActivo`.
- `Bounciness = 1` + `BounceCombine = Maximum` puede dar rebotes que ganan energía (la pelota sale más rápida de lo que entró). Bajarlo a ~0.85.
- Múltiples scripts (`PelotaBehaviour`, `BossAI`, `BallSpawner`, `SistemaDeServicio`, `RaquetaJugador`) escriben `isKinematic` y `useGravity` sin coordinarse. Necesitamos UN solo dueño del estado.

Gracias bro 🙏
