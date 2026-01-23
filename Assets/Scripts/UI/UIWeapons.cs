using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleFPS
{
	public class UIWeapons : MonoBehaviour
	{
	    public Image           WeaponIcon;
	    public Image           WeaponIconShadow;
	    public TextMeshProUGUI WeaponName;
		public TextMeshProUGUI ClipAmmo;
		public TextMeshProUGUI RemainingAmmo;
	    public Image           AmmoProgress;
	    public GameObject      NoAmmoGroup;
	    public CanvasGroup   WeaponThumbnail;
		public CanvasGroup BackWeaponThumbnail;

		private Weapon _weapon;
	    private int _lastClipAmmo;
	    private int _lastRemainingAmmo;

	    public void UpdateWeapons(Weapons weapons)
	    {
		    SetWeapon(weapons.CurrentWeapon);

			bool hasWeaponInHand = weapons.CurrentWeapon != null;
			WeaponThumbnail.alpha = hasWeaponInHand ? 1f : 0f;
			bool hasWeaponInBack = weapons.GetWeaponInBack() != null;
			BackWeaponThumbnail.alpha = hasWeaponInBack ? 1f : 0f;


			if (_weapon == null)
			    return;

		    UpdateAmmoProgress();

		    // Modify UI text only when value changed.
		    if (_weapon.AmmoInMagazin == _lastClipAmmo && _weapon.RemainingAmmo == _lastRemainingAmmo)
			    return;

		    ClipAmmo.text = _weapon.AmmoInMagazin.ToString();
		    RemainingAmmo.text = _weapon.RemainingAmmo < 1000 ? _weapon.RemainingAmmo.ToString() : "-";

		    NoAmmoGroup.SetActive(_weapon.AmmoInMagazin == 0 && _weapon.RemainingAmmo == 0);

		    _lastClipAmmo = _weapon.AmmoInMagazin;
		    _lastRemainingAmmo = _weapon.RemainingAmmo;
	    }

	    private void SetWeapon(Weapon weapon)
	    {
		    if (weapon == _weapon)
			    return;

		    _weapon = weapon;

		    if (weapon == null)
			    return;

		    WeaponIcon.sprite = weapon.Icon;
		    WeaponIconShadow.sprite = weapon.Icon;
			WeaponName.text = weapon.Name;
	    }

	    private void UpdateAmmoProgress()
	    {
		    if (_weapon.IsReloading)
		    {
			    // During reloading the ammo progress bar slowly fills.
			    AmmoProgress.fillAmount = _weapon.GetReloadProgress();
		    }
		    else
		    {
			    AmmoProgress.fillAmount = _weapon.AmmoInMagazin / (float)_weapon.MaxClipAmmo;
		    }
	    }
	}
}
