# Project Overview
- **Game Title:** Título no indicado; se conserva el proyecto existente (vaquero y zombis).
- **High-Level Concept:** Ampliar el plataformas 2D del desierto: el vaquero evita el contacto con un zombi y lo derrota únicamente al pisarlo descendiendo tras un salto. Mostrar las muertes, permitir reaparecer y simplificar las plataformas seleccionadas.
- **Players:** Un jugador local.
- **Inspiration / Reference Games:** No indicadas; mecánica clásica de pisotón de plataformas, sin introducir otras mecánicas.
- **Tone / Art Direction:** Conservar el desierto y los sprites existentes del vaquero y zombi; no generar arte nuevo.
- **Target Platform:** Windows de escritorio, StandaloneWindows64.
- **Screen Orientation / Resolution:** Horizontal, adaptación al tamaño de la ventana actual; no cambiar resolución del proyecto.
- **Render Pipeline:** Universal RP, Unity 6000.6.3f1.

## Decisiones del usuario
- Quitar «las más altas y la 1». Para esta revisión, se interpreta como las plataformas **1, 4, 5 y 6**: la situada a la izquierda del inicio y las tres superiores centrales. Se conserva 2, 3 y 7. Esta selección aparece explícitamente en la aprobación para poder corregirla antes de editar.
- Al morir, el vaquero reaparece al inicio, manteniendo derrotados los zombis ya eliminados. No recargar la escena.
- El zombi camina de lado a lado.

# Game Mechanics
## Core Gameplay Loop
Caminar y saltar, acercarse al zombi que patrulla, evitar tocarlo por los lados y caer sobre su cabeza para derrotarlo. Un contacto no válido mata al vaquero; tras la animación de muerte reaparece en su punto inicial y puede intentarlo de nuevo. El zombi muerto deja de causar daño y no revive al reaparecer el vaquero. La patrulla predecible y la respuesta visual clara favorecen aprendizaje y competencia (SDT); conservar algunas plataformas mantiene variedad del recorrido (MDA).

### Reglas de contacto
- Resolver el contacto en un único sistema, evitando que callbacks separados maten a ambos personajes.
- Pisotón válido: vaquero vivo, zombi vivo, vaquero descendiendo **antes de la resolución física**, pies previamente sobre la parte superior del zombi y contacto superior con solapamiento horizontal suficiente. Guardar posición/velocidad previa al paso físico; no confiar exclusivamente en la velocidad posterior al choque o en que el centro del vaquero esté más alto.
- Exigir que el descenso proceda de un salto iniciado por el jugador; caminar hacia un borde y caer sin saltar no debe derrotar al zombi.
- Un pisotón válido mata exclusivamente al zombi y aplica un pequeño rebote al vaquero, sin registrar daño ese mismo paso.
- Contacto lateral, desde abajo, estando en el suelo o subiendo: muerte del vaquero, sin matar al zombi.
- Muertes idempotentes: una sola animación y una sola reaparición, incluso con múltiples puntos de contacto.
- No añadir armas, daño por tiempo, ataque cuerpo a cuerpo ni vidas limitadas.

## Controls and Input Methods
- Backend verificado: New Input System (`activeInputHandler=1`).
- Conservar movimiento A/D y flechas; salto con espacio, W o flecha arriba; mando con stick/cruceta y botón sur.
- Al tocar la gestión de entrada del controlador, usar `InputAction` y callbacks de movimiento/salto con suscripciones en OnEnable/OnDisable, conservando aceleración, salto variable, coyote time y buffer existentes. No usar el backend antiguo ni cambiar Project Settings.
- Bloquear movimiento y salto mientras muere; limpiar entrada almacenada y buffers antes de reaparecer. Permitir continuar sin reiniciar el juego.

# UI
No crear menús, HUD, botones ni textos nuevos. La muerte y reaparición se comunican con las animaciones existentes.

Esquema visual (no es una UI nueva):
```text
+---------------------------------------+
|  Fondo del desierto                   |
|        cámara sigue al vaquero        |
|     vaquero        zombi patrullando   |
|________suelo____plataformas restantes_|
+---------------------------------------+
Muerte del vaquero -> animación -> inicio
Pisotón válido -> muerte del zombi -> sin daño
```

# Key Asset & Context
## Estado verificado
- Escena: `Assets/Scenes/SampleScene.unity`.
- Jugador `vaquerito`: SpriteRenderer, Animator, Rigidbody2D dinámico con interpolación y detección continua, CapsuleCollider2D, PlayerController y GroundCheck conectado. Suelo en layer Ground, máscara del jugador 64.
- `Assets/Scripts/PlayerController.cs`: movimiento, salto y animación, sin muerte; parámetros Animator Speed, IsGrounded y Jump.
- `Assets/Scripts/CameraFollow.cs`: ya está en Main Camera, habilitado y con target `vaquerito`. Tiene suavizado, zona muerta, anticipación y límites. No afirmar que falta el seguimiento; verificar y ajustar el existente.
- No existe un zombi en la escena inspeccionada. Sí existen sprites `Idle`, `Walk`, `Dead` y `Attack` en `Assets/Sprites/zombies/`.
- Vaquero: diez sprites Dead existentes en `Assets/Sprites/Vaquerito/`, PPU 256. Zombi: PPU 100, por lo que su escala debe ajustarse sin reimportaciones globales.
- Dependencias consultadas mediante AssetDatabase.GetDependencies y búsqueda inversa de escenas/prefabs/animaciones: los scripts actuales y el Animator del vaquero afectan a SampleScene; no se encontraron otros consumidores en esa búsqueda. Repetir análisis antes de editar y comprobar dependientes de sprites que se vayan a modificar.
- No se encontró `Assets/Plans/` al iniciar; este archivo constituye el plan inicial.

## Archivos a modificar o crear
- Modificar `Assets/Scripts/PlayerController.cs`: estado de muerte, reaparición, rastreo de salto/descenso y función de rebote; entrada por acciones conservando controles.
- Crear `Assets/Scripts/ZombieController.cs`: patrulla horizontal, resolución exclusiva del contacto y estado Dead.
- Modificar `Assets/Scripts/CameraFollow.cs` solo si las pruebas requieren corregir seguimiento/anticipación o añadir recentrado inmediato al reaparecer.
- Modificar `Assets/Animations/PlayerController.controller` y crear `Assets/Animations/Player_Dead.anim`.
- Crear `Assets/Animations/ZombieController.controller`, `Zombie_Idle.anim`, `Zombie_Walk.anim`, `Zombie_Dead.anim`.
- Modificar SampleScene: eliminar celdas seleccionadas, añadir un zombi de prueba sobre el suelo, conectar sus componentes, configurar cámara.
- Sprites y tiles existentes: reutilizar subassets importados, sin borrar imágenes, modificar paquetes ni cambiar materiales compartidos.

## Plataformas a retirar (celdas de Grid/Tilemap, inclusive)
1. Plataforma 1: x=-6..-3, y=0..1.
2. Plataforma 4: x=12..15, y=3..4.
3. Plataforma 5: x=17..20, y=5..6.
4. Plataforma 6: x=22..25, y=3..4.
Conservar íntegro el suelo x=-12..32, y=-4..-1 y las plataformas 2 (x=2..5), 3 (x=7..10) y 7 (x=27..30). No eliminar decoración ni assets de tiles.

# Implementation Steps
1. **Confirmar impacto y configuración antes de editar.** Revisar dependencias de los archivos afectados y sprites, pivotes y límites visibles; comprobar matriz de colisiones 2D y configuración del Tilemap/CompositeCollider2D. Registrar posición inicial del vaquero. Dependencias: aprobación de este plan.
2. **Simplificar nivel.** Editar únicamente las celdas enumeradas en SampleScene; regenerar/verificar geometría de colisión del Tilemap. Dependencias: paso 1.
3. **Preparar animaciones existentes.** Construir clips con SpriteRenderer.m_Sprite, ordenar frames numéricamente (1, 2, ... 10, no lexicográficamente); Idle/Walk en bucle y Dead sin bucle. Añadir Dead al Animator del vaquero sin alterar Idle/Run/Jump; evitar transiciones de salida durante la muerte. Mantener alineación de pies y tamaño visible en todas las animaciones. Dependencias: paso 1.
4. **Implementar muerte y reaparición.** En PlayerController añadir API clara como IsDead, Die() y BounceAfterStomp(), y estado de salto vigente. Al morir bloquear acciones y movimiento, mantener visible Dead, desactivar interacción dañina y esperar duración del clip más pausa corta configurable. Reubicar usando Rigidbody2D en el punto inicial, restaurar física/animación, poner velocidad cero y limpiar estados. No recargar escena ni resucitar zombis. Dependencias: pasos 1 y 3.
5. **Implementar zombi y contactos.** Crear ZombieController con patrulla por límites serializados, velocidad moderada configurable y raycasts de borde/pared con máscara Ground. Elegir resolución centralizada por contactos Collision2D con posición/velocidad previas: menos componentes y mejor sincronización con el controlador existente. Alternativa considerada: sensor de cabeza más trigger de daño, sencillo de visualizar pero vulnerable al orden de callbacks; no usar sensores independientes que resuelvan daño por separado. Ajustar normal del contacto según qué collider lo recibe y probar ambos sentidos. Dependencias: pasos 3 y 4.
6. **Integrar zombi.** Añadir un único zombi inicial aproximadamente en x=18, sobre suelo y separado del punto de reaparición, con SpriteRenderer, Animator, Rigidbody2D y collider ajustado al cuerpo visible. Ajustar escala respecto al vaquero; patrulla inicial aproximada x=16..21, validada contra el nivel real. Al morir detener patrulla, retirar collider dañino, reproducir Dead y retirar/desactivar el zombi al terminar. Dependencias: pasos 2, 3 y 5.
7. **Verificar y ajustar cámara existente.** Mantener target en el root móvil `vaquerito`, no en un hijo. Seguir en LateUpdate, conservar profundidad y suavizado; ajustar anticipación/límites si bloquean el encuadre al caminar o saltar. Añadir recentrado y limpieza de velocidad suavizada al reaparecer para evitar recorrido lento desde el lugar de muerte. No instalar Cinemachine ni duplicar cámaras. Dependencias: pasos 2, 4 y 6.
8. **Pruebas completas y guardado.** Ejecutar los casos siguientes, corregir regresiones, guardar escena/animaciones y revisar consola. Usar Undo para cambios de objetos y no ejecutar modificaciones mientras se revisa este plan. Dependencias: pasos 2–7.

# Verification & Testing
- Compilación sin errores nuevos ni componentes/referencias faltantes.
- A/D, flechas y mando funcionan; salto variable, buffer y coyote time siguen operativos; ausencia de teclado/mando no produce errores.
- Suelo completo conservado; solo desaparecen 1, 4, 5 y 6, incluidos sus colliders.
- Zombi visible y animado, escala/pies coherentes, patrulla y gira sin caer ni atravesar paredes.
- Tocar por izquierda/derecha o saltar contra su parte inferior mata únicamente al vaquero.
- Caer desde un salto sobre la cabeza mata únicamente al zombi y rebota al vaquero.
- Rozar un lateral mientras cae, tocar subiendo o caer sin haber saltado no mata al zombi.
- Probar normales del contacto, bordes de cabeza, distintas velocidades y múltiples contactos en un mismo paso; no muerte doble ni múltiples reapariciones.
- Dead del vaquero se reproduce completo; no puede caminar/saltar muerto; reaparece al inicio con física estable, cámara recentrada y sin salto almacenado.
- Derrotar zombi, morir posteriormente y reaparecer: ese zombi continúa eliminado.
- Zombi muerto no hace daño durante su animación ni después.
- Cámara sigue desplazamiento y saltos con jugador visible y sin vibraciones; revisar ventana panorámica actual y 16:9, extremos del suelo y reaparición.
- Revisar consola al entrar/salir de Play y al repetir muertes; comprobar que suscripciones de InputAction no se duplican.
