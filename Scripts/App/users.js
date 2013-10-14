function initUsers () {
	// =======================================================================
	// Make sure all of our content is hidden on startup
	$("#screenName").html("User Administration");

	// =======================================================================
	// =======================================================================
	// Beginning functionality for our Users screen
	//
	// In order for the MVVM to work properly
	// Observables need to be created in a document ready
	// 
	// Order of artifact creation is IMPORTANT!!!!
	//		1. Model
	//		2. DataSource
	//		3. ViewModel
	//		4. Bind the ViewModel
	// =======================================================================
	// =======================================================================


	// =======================================================================
	// Model and dataSource configurations
	// All models seem to need an id field in order for delete to work
	// =======================================================================
	var usersModel = kendo.data.Model.define({
		id: "UserName",
		fields: {
			"UserName": {
				type: "string",
				editable: "true",
				nullable: "false",
				validation: {
					required: true
				}
			},
			"FullName": {
				type: "string",
				editable: "true",
				nullable: "true"
			},
			"Email": {
				type: "string",
				editable: "true",
				nullable: "true"
			},
			"Password": {
				type: "string",
				editable: "true",
				nullable: "false",
				validation: {
					required: true
				}
			},
			"IsLockedOut": {
				type: "boolean",
				editable: "true",
				nullable: "true"
			}

		}
	});


	var userRolesModel = kendo.data.Model.define({
		id: "RoleName",
		fields: {
			"RoleName": {
				type: "string",
				editable: "true",
			},
			"UserName": {
				type: "string",
				editable: "true",
			}
		}
	});


	var usersDataSource = new kendo.data.DataSource({
		transport: {
			create: {
				url: "/rpc/account/CreateUser",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},
			read: {
				url: "/rpc/account/GetUsers",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "GET",
			},
			update: {
				url: "/rpc/account/UpdateUser",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "PUT"
			},
			destroy: {
				url: "/rpc/account/DeleteUser",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},

			// This is called last before sending the request 
			// I prefer here to put my data manipulations vs in the definition
			parameterMap: function (data, type) {

				if (type == "read") return null;

				// No data manipulation... just send back json
				return kendo.stringify(data);


			}
		},
		pageSize: 10,
		schema: {
			model: usersModel
		},

		// Fires when an error is returned from the server
		error: function (e) {
			if (e.xhr.status != 200) {
				//window.alert("Error: " + e.xhr.statusText);
			}
		}

	});


	var userRolesDataSource = new kendo.data.DataSource({
		transport: {
			create: {
				url: "/rpc/account/AddUserToRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},
			read: {
				url: function () {
					if (window.usersViewModel.get("selectedUserData") != null) {
						var url = "/rpc/account/GetUserRoles?id=";
						return (url + window.usersViewModel.get("selectedUserData.UserName")).toString();
					}
					return "";
				},
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "GET"
			},
			destroy: {
				url: "/rpc/account/RemoveUserFromRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},

			// This is called last before sending the request 
			// I prefer here to put my data manipulations vs in the definition
			parameterMap: function (data, type) {

				if (type == "read") return null;

				data.UserName = window.usersViewModel.get("selectedUserData.UserName");
				return kendo.stringify(data);
			}
		},
		pageSize: 10,
		schema: {
			model: userRolesModel
		},
		
		// Fires when an error is returned from the server
		error: function (e) {
			if (e.xhr.status != 200) {
				//window.alert("Error: " + e.xhr.statusText);
			}
		}



	});


	// =======================================================================
	// Beginning of Users Observables (ViewModel)
	// =======================================================================
	window.usersViewModel = kendo.observable({

		// Tracking of globals for the ViewModel
		selectedUserData: null,
		usersTabStripIsVisible: false,

		// References to our dataSources
		users: usersDataSource,
		userRoles: userRolesDataSource,

		// =======================================================================
		// When the grid starts to bind/rebinding with the datasource... 
		// Q. Causes of this event binding (dataBinding)?
		// A. Initial binding when grid is created, creating a new row, refresh the grid
		usersGridDataBinding: function (e) {

			// If we are binding, no user has been selected, hide the tabstrip, null the data
			this.set("usersTabStripIsVisible", false);
			this.set("selectedUserData", null);
			getRoles();
			$("#usersTabStrip").data("kendoTabStrip").select(0);
		},

		// =======================================================================
		// When a row is selected... 
		// Q. What causes a selection event (change)?
		// A. Actual row selection, clicking a button in a row, creating a new row
		usersGridChange: function (e) {

			// Get the selected row data items, kendo is kinda kludgy on how
			var selectedRow = e.sender.select();
			var rowData = e.sender.dataItem(selectedRow);

			// Set the observable for the row data items, show the tabstrip
			this.set("selectedUserData", rowData);
			this.set("usersTabStripIsVisible", true);

			// Have the other dataSources pull new rows
			this.userRoles.read();
		},

		// =======================================================================
		// When add/edit buttons are pressed
		// Q. What causes an edit event (edit)?
		// A. When the add or edit buttons are pressed
		usersGridEdit: function (e) {

			// If new user.. 
			if (e.model.isNew()) {
				// Change the title of our popup to reflect the button title
				e.container.data("kendoWindow").title("Create new user");
				
				// Since we are creating a new user... hide the IsLockedOut checkbox
				hideFieldInPopup("IsLockedOut", e.container);
			} else {
				// else cannot change UserName, also password is now optional
				e.container.data("kendoWindow").title("Edit user");
				changeAttributeOfPopupField("UserName", e.container, "readOnly", true);
				removeAttributeFromPopupField("Password", e.container, "required");
			}
		},

		// =======================================================================
		// When add/edit buttons are pressed
		userAssignedRolesGridEdit: function (e) {
			// Change the title of our popup window to align with our button text
			e.container.data("kendoWindow").title("Add role to user");

			// Make sure RoleName field is required
			changeAttributeOfPopupField("RoleName", e.container, "required", true);

			// If we are editing an existing UserAssignedRole, populate the UserName of the model
			// Since we removed the edit button, this is redundant, only for new records
			if (!e.model.isNew()) {
				e.model.UserName = this.get("selectedUserData.UserName");
			};
		},

		// =======================================================================
		// Format UserData date fields for local display
		formatUserDataToDate: function (field) {
			if (this.get("selectedUserData") != null) {
				return kendo.parseDate(this.get(("selectedUserData." + field)));
			};
			return field;
		},

		// =======================================================================
		// End of our users ViewModel
	});


	// =======================================================================
	// Finally... bind our view to the users screen object
	kendo.bind($("#usersScreen"), window.usersViewModel);

	// =======================================================================
	// =======================================================================
	// End of Users functionality
	// =======================================================================
	// =======================================================================

};

// =======================================================================
// Custom editor for assigning a role to a user dropdown
// Your datasource needs to be in the observable object, or in the global scope (window)
//		options.field = Grid field name for this editor
//		dataSource = obvious, local or remote
//		dataBind = Grid field name, will be binding with it
//		dataTextField = to be displayed in our dropdown, friendly
//		dataValueField = what value when selected will be saved to the dataBind field
function assignRoleToUserEditor(container, options) {
	getRoles();
	$("<input required name='" + options.field + "'/>")
	.appendTo(container)
	.kendoDropDownList({
		dataSource: window.roles,
		dataBind: "value: " + options.field,
		dataTextField: 'RoleName',
		dataValueField: 'RoleName',
		optionLabel: "Select a role"
	});
};

// =======================================================================
// Hide a field from being displayed in a popup editor
function hideFieldInPopup(fieldName, container) {

	// Get our input for the field
	var fieldCol = $("input[name=" + fieldName + "]", container);
	fieldCol.parent().hide();

	// Get our label for the field
	fieldCol = $("label[for=" + fieldName + "]", container);
	fieldCol.parent().hide();
};

// =======================================================================
// Change an attribute of a field in a popup editor
function changeAttributeOfPopupField(fieldName, container, attribute, value) {
	var fieldCol = $("input[name=" + fieldName + "]", container);
	fieldCol.attr(attribute, value);
};

// =======================================================================
// Remove an attribute of a field in a popup editor
function removeAttributeFromPopupField(fieldName, container, attribute) {
	var fieldCol = $("input[name=" + fieldName + "]", container);
	fieldCol.removeAttr(attribute);
};

// Initialize our application permisison list, as these don't change at runtime
function getRoles() {
	$.getJSON("/rpc/account/GetRoles").done(function (jsonData) {
		window.roles = jsonData;
	});
};



