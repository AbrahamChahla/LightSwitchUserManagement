
function initRoles() {
	// =======================================================================
	// Beginning of functionality for our Roles screen
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
	$("#screenName").html("Role Administration");


	// =======================================================================
	// Model and dataSource configurations
	// All models seem to need an id field in order for delete to work
	// =======================================================================
	var rolePermissionsModel = kendo.data.Model.define({
		id: "PermissionId",
		fields: {
			"RoleName": {
				type: "string",
				editable: "true",
				nullable: "true"
			},
			"PermissionId": {
				type: "string",
				editable: "true"
			}
		}
	});


	// =======================================================================
	var usersInRoleModel = kendo.data.Model.define({
		id: "UserName",
		fields: {
			"UserName": {
				type: "string",
				editable: "true"
			},
			"RoleName": {
				type: "string",
				editable: true
			}
		}
	});


	// =======================================================================
	var rolesModel = kendo.data.Model.define({
		id: "RoleName",
		fields: {
			"RoleName": {
				type: "string",
				editable: "true",
				nullable: "false",
				validation: {
					required: true
				}
			}
		}
	});


	// =======================================================================
	var rolesDataSource = new kendo.data.DataSource({
		transport: {
			create: {
				url: "/rpc/account/CreateRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},
			read: {
				url: "/rpc/account/GetRoles",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "GET"
			},
			update: {
				url: "/rpc/account/UpdateRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "PUT"
			},
			destroy: {
				url: "/rpc/account/DeleteRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},
			parameterMap: function (data, type) {

				if (type == "read") return null;
				
				return kendo.stringify(data);
			}
		},
		pageSize: 10,
		schema: {
			model: rolesModel
		},

		// Fires when an error is returned from the server
		error: function (e) {
			if (e.xhr.status != 200) {
				//window.alert("Error: " + e.xhr.statusText);
			}
		}

	});


	// =======================================================================
	var rolePermissionsDataSource = new kendo.data.DataSource({
		transport: {
			create: {
				url: "/rpc/account/AddPermissionToRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},
			read: {
				url: function () {
					if (window.rolesViewModel.get("selectedRoleName") != null) {
						var url = "/rpc/account/GetRolePermissions?id=";
						return (url + window.rolesViewModel.get("selectedRoleName")).toString();
					}
					return null;
				},
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "GET"
			},
			destroy: {
				url: "/rpc/account/RemovePermissionFromRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},
			parameterMap: function (data, type) {

				if (type == "read") return null;

				data.RoleName = window.rolesViewModel.get("selectedRoleName");
				return kendo.stringify(data);

			}
		},
		pageSize: 10,
		schema: {
			model: rolePermissionsModel
		},

		// Fires when an error is returned from the server
		error: function (e) {
			if (e.xhr.status != 200) {
				//window.alert("Error: " + e.xhr.statusText);
			};
		}

	});


	// =======================================================================
	var usersInRoleDataSource = new kendo.data.DataSource({
		transport: {
			create: {
				url: "/rpc/account/AddUserToRole",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				type: "POST"
			},
			read: {
				url: function () {
					if (window.rolesViewModel.get("selectedRoleName") != null) {
						var url = "/rpc/account/GetUsersInRole?id=";
						return (url + window.rolesViewModel.get("selectedRoleName")).toString();
					}
					return null;
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
			parameterMap: function (data, type) {

				if (type == "read") return null;
				
				data.RoleName = window.rolesViewModel.get("selectedRoleName");
				return kendo.stringify(data);
			}
		},
		pageSize: 10,
		schema: {
			model: usersInRoleModel
		},

		// Fires when an error is returned from the server
		error: function (e) {
			if (e.xhr.status != 200) {
				//window.alert("Error: " + e.xhr.statusText);
			}
		}

	});


	// =======================================================================
	// Beginning of Roles Observables (ViewModel)
	// =======================================================================
	window.rolesViewModel = kendo.observable({
		//Tracking of globals for the ViewModel
		selectedRoleName: null,
		rolesTabStripIsVisible: false,

		// References to our dataSources
		roles: rolesDataSource,
		rolePermissions: rolePermissionsDataSource,
		usersInRole: usersInRoleDataSource,

		// =======================================================================
		// When the grid starts to bind/rebinding with the datasource... causes?
		// Initial binding when grid is created, creating a new row, refresh the grid
		rolesGridDataBinding: function (e) {

			// If we are binding, no role has been selected, hide the tabstrip, null the data
			this.set("selectedRoleName", null);
			this.set("rolesTabStripIsVisible", false);
			getUsers();
			$("#rolesTabStrip").data("kendoTabStrip").select(0);
		},

		// =======================================================================
		// When a row is selected... what causes a selection?
		// Actual row selection, clicking a button in a row, creating a new row
		rolesGridChange: function (e) {

			// Get the selected row data items, kendo is kinda kludgy on how
			var selectedRow = e.sender.select();
			var rowData = e.sender.dataItem(selectedRow);

			// Set the observable for the row data items, show the tabstrip
			this.set("selectedRoleName", rowData.RoleName),
			this.set("rolesTabStripIsVisible", true);

			// Have the other dataSources pull new rows
			this.rolePermissions.read();
			this.usersInRole.read();
		},

		// =======================================================================
		// When Create a new role button press
		rolesGridEdit: function (e) {

			// Change the title of our popup to reflect the button title
			e.container.data("kendoWindow").title("Create new role");
		},

		// =======================================================================
		// When assign a role permission button press
		rolePermissionsGridEdit: function (e) {

			// Change the title of our popup to reflect the button title
			e.container.data("kendoWindow").title("Add permission to role");
			changeAttributeOfPopupField("PermissionId", e.container, "required", true);
		},

		// =======================================================================
		// When Assign a user button press
		usersInRoleGridEdit: function (e) {

			// Change the title of our popup to reflect the button title
			e.container.data("kendoWindow").title("Add user to role");
			changeAttributeOfPopupField("UserName", e.container, "required", true);
		}

		// =======================================================================
		// End of our roles ViewModel
	});


	// =======================================================================
	// Finally... bind our view to the roles screen object
	kendo.bind($("#rolesScreen"), window.rolesViewModel);

	// =======================================================================
	// =======================================================================
	// End of roles functionality 
	// =======================================================================
	// =======================================================================


	// =======================================================================
	// Misc required items during startup
	// =======================================================================

	// Initialize our application permisison list, as these don't change at runtime
	$.getJSON("/rpc/account/GetApplicationPermissions", function (data) {
		window.applicationPermissions = data;
	});

};
// =======================================================================
// End of document ready


// =======================================================================
// Custom editor for assigning a permission to a role dropdown
// Your datasource needs to be in the observable object, or in the global scope (window)
//		options.field = Grid field name for this editor, in your grid configuration
//		dataSource = obvious, local, remote, observable
//		dataBind = Grid field name, will be binding it with, options.field
//		dataTextField = to be displayed in our dropdown, friendly
//		dataValueField = what value when selected will be saved to the dataBind field
function assignPermissionToRoleEditor(container, options) {
	$("<input required name='" + options.field + "'/>")
	.appendTo(container)
	.kendoDropDownList({
		dataSource: window.applicationPermissions,
		dataBind: "value: " + options.field,
		dataTextField: 'Name',
		dataValueField: 'Id',
		optionLabel: "Select a permission"
	});
};


// Resolve the PermissionId for display in the grid vs the Id
// Used as a template for the column we are doing the dropdown for
function getPermissionName(permissionId) {
	var permissionName = "";
	if (permissionId != undefined) {
		permissionName = $.map(window.applicationPermissions, function (perm) {
			if (perm.Id == permissionId)
				return perm.Name;
		});
	};
	return permissionName;
};

// =======================================================================
// Custom editor for assigning a role to a user dropdown
// Your datasource needs to be in the observable object, or in the global scope (window)
//		options.field = Grid field name for this editor, in your grid configuration
//		dataSource = obvious, local, remote, observable
//		dataBind = Grid field name, will be binding it with, options.field
//		dataTextField = to be displayed in our dropdown, friendly
//		dataValueField = what value when selected will be saved to the dataBind field
function assignUserToRoleEditor(container, options) {
	$("<input required name='" + options.field + "'/>")
	.appendTo(container)
	.kendoDropDownList({
		dataSource: window.users,
		dataBind: "value: " + options.field,
		dataTextField: 'UserName',
		dataValueField: 'UserName',
		optionLabel: "Select a user"
	});
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
	$("<input required name='" + options.field + "'/>")
	.appendTo(container)
	.kendoDropDownList({
		dataSource: window.rolesViewModel.get("roles"),
		dataBind: "value: " + options.field,
		dataTextField: 'RoleName',
		dataValueField: 'RoleName',
		optionLabel: "Select a role"
	});
};


// =======================================================================
// =======================================================================
// Helper functions
// =======================================================================
// =======================================================================


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

function getUsers() {
	$.getJSON("/rpc/account/GetUsers").done(function (jsonData) {
		window.users = jsonData;
	});
};

