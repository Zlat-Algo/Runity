using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviourPlus
{
    public float speed;
    public GameObject bulletPrefab;

    void Start()
    {

    }

    void OnMove(InputValue value)
    {
        физическоеТело2D.движение = value.GetVector2() * speed;
    }

    void OnShoot()
    {
        GameObject bullet = Заспавнить(bulletPrefab).УстановитьПоворот2D(45);
        bullet.GetComponent<Rigidbody2D>().linearVelocity = mouseWorldDirection;
        bullet.transform.localScale *= 0.5f;
    }

    void OnShootHold()
    {
        отрисовщикСпрайта.УстановитьЦвет(Color.wheat);
    }

    void OnMegaShoot()
    {
        Заспавнить(bulletPrefab, transform.position, mouseWorldPosition, true).НайтиКомпонент<ФизическоеТело2D>().движение = mouseWorldDirection;
        НайтиКомпонент<SpriteRenderer>().color = Color.white;
    }

    void Update()
    {
        //трансформация.УстановитьX(6);
    }
}
