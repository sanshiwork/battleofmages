﻿using UnityEngine;
using UnityEngine.UI;
using uLobby;
using System;
using BoM.UI;
using BoM.UI.Notifications;

public class Register : SingletonMonoBehaviour<Register>, Initializable {
	public InputField emailField;
	public InputField passwordField;
	public InputField loginEmailField;
	public Button registerButton;

	// Init
	public void Init() {
		// Add this class as a listener to different account events
		AccountManager.OnAccountRegistered += OnAccountRegistered;
		AccountManager.OnRegisterFailed += OnRegisterFailed;

		// Receive lobby events
		Lobby.AddListener(this);
	}

	// OnEnable
	void OnEnable() {
		// Load login field text
		emailField.text = loginEmailField.text;

		// Validate
		Validate();
	}

	// CreateNewAccount
	public void CreateNewAccount() {
		LogManager.General.Log("Requesting to create a new account: " + emailField.text);

		// Request lobby to register a new account
		Lobby.RPC("AccountRegister", Lobby.lobby, emailField.text, GameDB.EncryptPasswordString(passwordField.text));
	}

	// Validate
	public void Validate() {
		/*registerButton.interactable =
			emailField.GetComponent<InputFieldValidator>().valid &&
			passwordField.GetComponent<InputFieldValidator>().valid;*/
	}
	
#region Callbacks
	// OnAccountRegistered
	void OnAccountRegistered(Account account) {
		LogManager.General.Log("Registered account: " + account);

		// Create a notification
		NotificationManager.instance.CreateNotification("You have successfully registered!");

		// Send the player back to the main menu
		UIManager.instance.currentState = "Login";
	}
	
	// OnRegisterFailed
	void OnRegisterFailed(string accountName, AccountError error) {
		LogManager.General.LogWarning("Account registration failed: " + accountName + " (" + error + ")");

		// Create a notification
		NotificationManager.instance.CreateNotification("Registration failed: " + error);
	}

	// EmailAlreadyExists
	[RPC]
	void EmailAlreadyExists() {
		LogManager.General.LogWarning("Account registration failed: Email already exists");

		// Create a notification
		NotificationManager.instance.CreateNotification("Registration failed: Email already exists");
	}
#endregion

	[RPC]
	void ReceiveAccountInfo(string accountId, string propertyName, string typeName, string json) {
		LogManager.General.Log("Received " + propertyName + ": " + json);

		var val = GenericSerializer.ReadObject(Type.GetType(typeName), json);
		var account = PlayerAccount.Get(accountId);

		var propertyField = account.GetType().GetField(propertyName);
		var property = propertyField.GetValue(account);
		var propertyType = propertyField.FieldType;
		var valueProperty = propertyType.GetProperty("value");

		valueProperty.SetValue(property, val, null);

		if(propertyName == "playerName") {
			// Add name to account dictionary
			if(!string.IsNullOrEmpty((string)val))
				PlayerAccount.playerNameToAccount[(string)val] = account;

			// Next page
			if(account.isMine && (UIManager.instance.currentState == "Waiting" || UIManager.instance.currentState == "Ask Name")) {
				if(string.IsNullOrEmpty((string)val))
					UIManager.instance.currentState = "Ask Name";
				else
					UIManager.instance.currentState = "Lobby";
			}
		}
	}
}
