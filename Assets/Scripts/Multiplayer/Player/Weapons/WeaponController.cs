using UnityEngine;
using Riptide;
using TMPro;

public class WeaponController : MonoBehaviour
{
    public GameObject[] allGuns;
    public int activeWeaponIndex = -1;

    public Gun currentGun { get; private set; }
    public GameObject currentGunModel;

    private PlayerInventory inventory;
    private int lastEquippedGunIndex = -2;

    public void Start()
    {
        inventory = GetComponentInParent<Player>()?.inventory ?? GetComponent<PlayerInventory>();
        HideAllGuns();
    }

    void Update()
    {
        if (inventory == null) return;

        ItemData equipped = inventory.GetEquippedItem();
        int gunIndex = equipped is WeaponData wd ? wd.gunIndex : -1;

        if (gunIndex != lastEquippedGunIndex)
        {
            lastEquippedGunIndex = gunIndex;
            if (gunIndex >= 0)
                EquipWeapon(gunIndex);
            else
                HideAllGuns();
        }
    }

    public void EquipWeapon(int index)
    {
        if (allGuns == null || allGuns.Length == 0) return;
        if (index < 0 || index >= allGuns.Length)
        {
            HideAllGuns();
            return;
        }

        foreach (GameObject gun in allGuns)
        {
            if (gun != null) gun.SetActive(false);
        }

        activeWeaponIndex = index;
        currentGun = allGuns[index].GetComponent<Gun>();
        currentGunModel = allGuns[index];
        if (currentGunModel != null) currentGunModel.SetActive(true);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.Client != null && NetworkManager.Singleton.Client.IsConnected)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.WeaponChange);
            message.AddInt(index);
            NetworkManager.Singleton.Client.Send(message);
        }
    }

    public void HideAllGuns()
    {
        lastEquippedGunIndex = -1;
        activeWeaponIndex = -1;
        currentGun = null;
        currentGunModel = null;

        if (allGuns != null)
        {
            foreach (GameObject gun in allGuns)
            {
                if (gun != null) gun.SetActive(false);
            }
        }
    }
}