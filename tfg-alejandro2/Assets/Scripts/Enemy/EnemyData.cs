using UnityEngine;
using System.Collections.Generic; // Necesario para List

// --- NUEVO: Estructura para definir un posible drop ---
// [System.Serializable] hace que esta estructura aparezca en el Inspector de Unity.
[System.Serializable]
public struct ItemDropInfo
{
    [Tooltip("El objeto que podr�a soltar.")]
    public ItemData item;
    [Tooltip("La probabilidad (de 0.0 a 1.0) de que este objeto sea soltado.")]
    [Range(0f, 1f)]
    public float chance;
}
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "TuJuego/Crear Datos de Enemigo")]
public class EnemyData : ScriptableObject
{
    [Header("Informaci�n General del Enemigo")]
    [Tooltip("Identificador �nico para este tipo de enemigo (ej: 'goblin_warrior', 'slime_blue').")]
    public string enemyID;

    [Tooltip("Nombre del enemigo tal como se mostrar� al jugador (ej: 'Limo', 'Guerrero Goblin').")]
    public string enemyName = "Nuevo Enemigo";

    [Tooltip("Sprite que se usar� para este enemigo en la pantalla de combate (vista 3/4, 48x48 p�xeles).")]
    public Sprite battleSprite; // El sprite de 48x48 para el combate

    // --- NUEVO: Animator Controller para el combate espec�fico del enemigo ---
    [Tooltip("Animator Controller a usar para este tipo de enemigo en combate. Debe contener estados como Idle_Combat, AttackTrigger, HitTrigger, DefeatTrigger, etc.")]
    public RuntimeAnimatorController combatAnimatorController;

    // Podr�as a�adir aqu� un 'explorationSprite' si el sprite en el mapa es diferente al del combate,
    // aunque el GameObject en el mapa ya tendr� su propio SpriteRenderer.

    [Header("Estad�sticas de Combate Base")]
    [Tooltip("Puntos de Vida (HP) m�ximos del enemigo.")]
    public int maxHP = 50;
    [Tooltip("Ataque base del enemigo.")]
    public int baseAttack = 8;
    [Tooltip("Defensa base del enemigo.")]
    public int baseDefense = 3;
    [Tooltip("Ataque M�gico base del enemigo, si aplica.")]
    public int baseMagicAttack = 0;
    [Tooltip("Defensa M�gica base del enemigo, si aplica.")]
    public int baseMagicDefense = 0;
    [Tooltip("Velocidad base del enemigo, para determinar el orden de turno.")]
    public int baseSpeed = 5;
    // Podr�as a�adir m�s stats como Evasi�n, Punter�a, resistencias elementales, etc.

    [Header("Comportamiento y Habilidades")]
    [Tooltip("Si est� marcado, este enemigo usar� una IA de jefe (usar� habilidades).")]
    public bool isBoss = false; // --- NUEVO: Bandera para identificar jefes ---
    [Tooltip("Lista de habilidades que este enemigo puede usar si es un jefe.")]
    public List<AbilityData> abilities = new List<AbilityData>();

    [Header("Recompensas al Ser Derrotado")]
    [Tooltip("Cantidad de Puntos de Experiencia (XP) que el jugador obtiene al derrotar a este enemigo.")]
    public int xpReward = 10;

    // --- NUEVO: Campos para Sonidos de Combate ---
    [Header("Sonidos de Combate")]
    [Tooltip("Sonido que se reproduce cuando este enemigo realiza un ataque b�sico.")]
    public AudioClip basicAttackSound;
    [Tooltip("Sonido que se reproduce cuando este enemigo recibe un golpe.")]
    public AudioClip takeHitSound;
    [Tooltip("Sonido que se reproduce cuando este enemigo es derrotado.")]
    public AudioClip defeatSound;
    // --- FIN NUEVO ---
    // --- L�GICA DE DROP ACTUALIZADA ---
    [Header("Objetos que Puede Soltar (Loot)")]
    [Tooltip("Lista de todos los objetos que este enemigo podr�a soltar. El juego revisar� esta lista en orden y soltar� el PRIMERO que cumpla su probabilidad.")]
    public List<ItemDropInfo> potentialDrops = new List<ItemDropInfo>();

    // Ejemplo m�s avanzado para m�ltiples drops con diferentes probabilidades:
    // public List<ItemDropInfo> potentialDrops = new List<ItemDropInfo>();
    // [System.Serializable]
    // public class ItemDropInfo {
    //    public ItemData item;
    //    [Range(0f, 1f)] public float chance;
    //    public int minQuantity = 1;
    //    public int maxQuantity = 1;
    // }

    // Podr�as a�adir aqu� campos para resistencias/debilidades elementales,
    // tipo de enemigo (para fortalezas/debilidades), etc.
}
