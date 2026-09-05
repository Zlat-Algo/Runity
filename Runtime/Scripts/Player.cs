using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviourPlus
{
    public float скорость;
    public GameObject префабПули;

    void Start()
    {

    }

    void OnMove(InputValue value)
    {
        физическоеТело2D.движение = value.ИзвлечьV2() * скорость;
    }

    void OnShoot()
    {
        GameObject новаяПуля = Заспавнить(префабПули).УстановитьПоворот2D(45);
        новаяПуля.НайтиКомпонент<ФизическоеТело2D>().движение = Направление2D(1, 0);
        новаяПуля.transform.localScale *= 0.5f;
    }

    void OnShootHold()
    {
        отрисовщикСпрайта.УстановитьЦвет(Цвета.пшеничный);
    }

    void OnMegaShoot()
    {
        Заспавнить(префабПули, transform.position, mouseWorldPosition, да).НайтиКомпонент<ФизическоеТело2D>().движение = Направление2D(1, 0);
        НайтиКомпонент<Спрайтер>().цвет = Цвета.белый;
    }

    void Update()
    {
        if (Управление.вперёдУдерживается)
        {
            Консоль.Напечатать("Бегу, дорогуша");
        }
        else if (Управление.назадУдерживается)
        {
            Консоль.Напечатать("Да ну нафиг");
        }
    }
}
