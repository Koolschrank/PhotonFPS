using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleFPS
{
	public class UIWeapons : MonoBehaviour
	{
	    public Image           WeaponIcon;
	    public Image           WeaponIconShadow;
		public TextMeshProUGUI ClipAmmo;
		public TextMeshProUGUI RemainingAmmo;
	    public Image           AmmoProgress;
	    public GameObject      NoAmmoGroup;
		public Image BackWeaponIcon;

		private EWeaponType _lastweaponType;
	    private int _lastAmmoInMagazin;
	    private int _lastReserveAmmo;

	    public void UpdateWeapons(Weapons weapons)
	    {

		    SetWeapon(weapons.ActiveWeapon);
			UpdateAmmoProgress(weapons.ActiveWeapon);
			SetBackWeapon(weapons.BackWeapon);
		}

	    private void SetWeapon(WeaponState weapon)
	    {
			if ( weapon.WeaponType == _lastweaponType)
			{
				return;
			}
			if ( weapon.WeaponType == EWeaponType.None )
			{
				WeaponIcon.enabled = false;
				WeaponIconShadow.enabled = false;
				return;
			}
			else
			{
				WeaponIcon.enabled = true;
				WeaponIconShadow.enabled = true;
			}

			var weaponData = WeaponDatabase.weaponList.GetWeaponData(weapon.WeaponType);
			_lastweaponType = weapon.WeaponType;

		    WeaponIcon.sprite = weaponData.weaponIcon;
		    WeaponIconShadow.sprite = weaponData.weaponIcon;
	    }

	    private void UpdateAmmoProgress(WeaponState weapon)
	    {
			if ( weapon.WeaponType == EWeaponType.None )
			{
				ClipAmmo.text = "-";
				RemainingAmmo.text = "-";
				AmmoProgress.fillAmount = 0;
				return;
			}

			if (weapon.AmmoReserve != _lastReserveAmmo)
			{
				_lastReserveAmmo = weapon.AmmoReserve;
			}
			if (weapon.AmmoInMagazin != _lastAmmoInMagazin)
			{
				_lastAmmoInMagazin = weapon.AmmoInMagazin;
			}
			ClipAmmo.text = _lastAmmoInMagazin.ToString();
			RemainingAmmo.text = _lastReserveAmmo.ToString();
			int maxMagazinSize = WeaponDatabase.weaponList.GetWeaponData(weapon.WeaponType).MagazinSize;

			AmmoProgress.fillAmount = weapon.AmmoInMagazin / (float)maxMagazinSize;
		}

		public void SetBackWeapon(WeaponState weapon)
		{
			// none
			if ( weapon.WeaponType == EWeaponType.None )
			{
				BackWeaponIcon.enabled = false;
				return;
			}
			BackWeaponIcon.enabled = true;

			var weaponData = WeaponDatabase.weaponList.GetWeaponData(weapon.WeaponType);
			BackWeaponIcon.sprite = weaponData.weaponIcon;
		}
	}
}
