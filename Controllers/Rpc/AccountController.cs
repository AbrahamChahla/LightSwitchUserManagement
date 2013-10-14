// =======================================================================================================
// Platform:		ASP.NET Web.API and Visual Studio LightSwitch 2013 HTMLClient
//					Used with Web Form authentication, not for Windows, could be expanded tho
// Class:			AccountController
// Dependencies:	AutoMapper
//					System.Web.ApplicationServices
// Objective:		Allow management of User Registrations, Roles and Permissions
// URL:				/api/account/{action}/{id}
// Post Data:		Complex types are expected in the body of the requests
// Return Data:		Always be Json, embedded in the response
// Author:			Dale Morrison, Interbay Technology Group, LLC @ http://blog.ofanitguy.com
//
// Comments:		We rely heavily on DTOs (Data Transfer Objects)
//					We also rely on try/catch to simplify logic
//					Small modifications will allow you to get to all the Membership data
//					Converts easily to server side controller
//
// Refactor:		GetUsers, GetUsersInRole to allow pagination, for now, leaving it up to the client
//
// Last Modified:	10/13/2013
// =======================================================================================================

#region Includes

// Auto generated includes
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

// Controller specific includes
using System.Web.Security;
using Microsoft.LightSwitch.Security;
using Microsoft.LightSwitch.Security.ServerGenerated.Implementation;
using Microsoft.LightSwitch;
using LightSwitchApplication.DTOs;

#endregion Includes

namespace LightSwitchApplication.Controllers
{
	[RoutePrefix("/rpc")]
	// [RequireHttps]
	[Authorize(Roles = "Administrator")]
	public class AccountController : ApiController
	{

		// =======================================================================================================
		// Initialize our Controller if necessary
		// =======================================================================================================
//		public AccountController()
//		{
//
//		}

		
		// =======================================================================================================
		// GetUsers - Get list of all registered users
		// Verb: Get
		// Data: None
		// Returns: Json list of users, expanded version of the LightSwitch user
		// =======================================================================================================
		[HttpGet]
		public IEnumerable<ExpandedUserDTO> GetUsers()
		{
			try
			{
				// Make sure we have a ServerApplicationContext, if unable to, throw an exception
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null)
					throw new Exception("Could not create a ServerApplicationContext... SAC1");

				using (ServerApplicationContext.Current)
				{
					// Go get the LightSwitch profile data for all users
					var lsUsers = ServerApplicationContext.Current.DataWorkspace.SecurityData
						.UserRegistrations
						.Select(u => new { u.UserName, u.FullName})
						.Execute();

					// Get all our other data for all users
					var membershipUsers = Membership.GetAllUsers().Cast<MembershipUser>();

					// Now lets join the two sets
					var result = from member in membershipUsers
							 from lsUser in lsUsers
							 where member.UserName == lsUser.UserName
							 select new ExpandedUserDTO
							 {
								 UserName = member.UserName,
								 FullName = lsUser.FullName,
								 Email = member.Email,
								 IsOnline = member.IsOnline,
								 IsLockedOut = member.IsLockedOut,
								 LastLoginDate = member.LastLoginDate,
								 LastPasswordChangeDate = member.LastPasswordChangedDate,
								 CreationDate = member.CreationDate
							 };

					return result;
				}

			}
			catch (Exception e)
			{
				// If there was some failure return an error response, client will deal with it
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// GetUser - Get the data of an individual registered user
		// Verb: Get
		// Data: id (UserName)
		// Returns: Json representation of an individual user
		// =======================================================================================================
		[HttpGet]
		public ExpandedUserDTO GetUser(string id)
		{
			try
			{
				// Make sure we have a ServerApplicationContext, if unable to, throw an exception
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null)
					throw new Exception("Could not create a ServerApplicationContext... SAC2");

				using (ServerApplicationContext.Current)
				{
					// Go get our LightSwitch user data
					var result = ServerApplicationContext.Current.DataWorkspace.SecurityData
						.UserRegistrations
						.Where(u => u.UserName == id)
						.Select(u => new ExpandedUserDTO
						{
							UserName = u.UserName,
							FullName = u.FullName
						}).Execute().First();

					// Get our ASP Membership data
					var member = Membership.GetUser(id);

					// If we could not find the user, throw an exception
					if (member == null) throw new Exception("Could not find the User");

					// Stuff the asp data into our DTO
					result.Email = member.Email;
					result.CreationDate = member.CreationDate;
					result.IsOnline = member.IsOnline;
					result.LastLoginDate = member.LastLoginDate;
					result.LastPasswordChangeDate = member.LastPasswordChangedDate;

					return result;
				}

			}
			catch (Exception e)
			{
				// Again... if anything fails we return an error response for the client to deal with
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// GetExpandedUser - Get the data of an individual registered user
		// Verb: Get
		// Data: id (UserName)
		// Returns: Json representation of an individual user, Expanded version
		// Comment: Might be a better way of getting the roles/permissions
		// =======================================================================================================
		[HttpGet]
		public ExpandedUserDTO GetExpandedUser(string id)
		{
			try
			{
				// Make sure we have a ServerApplicationContext, if unable to, throw an exception
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null)
					throw new Exception("Could not create a ServerApplicationContext... SAC3");

				using (ServerApplicationContext.Current)
				{
					var dbContext = ServerApplicationContext.Current.DataWorkspace;

					// Get Our user data from LightSwitch
					var result = dbContext.SecurityData
						.UserRegistrations
						.Where(u => u.UserName == id)
						.Select(u => new ExpandedUserDTO
						{
							UserName = u.UserName,
							FullName = u.FullName
						}).Execute().First();

					// Get the expanded data from the membership provider
					var member = Membership.GetUser(id);

					// Populate our DTO
					if (member != null)
					{
						result.Email = member.Email;
						result.CreationDate = member.CreationDate;
						result.LastLoginDate = member.LastLoginDate;
						result.LastPasswordChangeDate = member.LastPasswordChangedDate;
						result.IsOnline = member.IsOnline;
					}

					// Get all the roles assigned to this user
					var roles = dbContext.SecurityData.RoleAssignments.Where(r => r.UserName == id).Select(r => r.Role).Execute();

					// Lets go thru each role object and build a composite for transfer
					var expandedRoles = roles.Select(r => new UserRolesDTO
					{
						RoleName = r.Name, 
						Permissions = r.RolePermissions.Select(p => new PermissionDTO {Name = p.Permission.Name, Id = p.PermissionId})
					}).ToList();

					// Add the expanded roles to the expanded users
					result.Roles = expandedRoles;

					return result;
				}

			}
			catch
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// GetRoles - Get list of all the roles in the system
		// Verb: Get
		// Data: None
		// Returns: Json list of strings with the names
		// =======================================================================================================
		[HttpGet]
		public IEnumerable<RoleDTO> GetRoles()
		{
			try
			{
				// Simple.. go get the roles
				var result = (from r in Roles.GetAllRoles() 
							  select new RoleDTO { RoleName = r });
				return result;
			}
			catch
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, "No roles found");
				throw new HttpResponseException(response);
			}

		}


		// =======================================================================================================
		// GetUserRoles - Get a list of all the roles associated with a user
		// Verb: Get
		// Data: id (UserName)
		// Returns:  Json list roles the user is associated
		// =======================================================================================================
		[HttpGet]
		public IEnumerable<UserRoleDTO> GetUserRoles(string id)
		{
			try
			{
				// Go get the roles for an individual
				var result = (from r in Roles.GetRolesForUser(id)
							  select new UserRoleDTO { RoleName = r, UserName = id });

				return result;
			}
			catch
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, "No roles assigned to user");
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// GetApplicationPermissions - Get a list of application wide permissions
		// Verb: Get
		// Data: None
		// Returns:  Json list of permissions, application wide.  Id and Name
		// =======================================================================================================
		[HttpGet]
		public IEnumerable<PermissionDTO> GetApplicationPermissions()
		{
			try
			{
				// Call our internal helper function that gets the LS permissions
				var result = GetLightSwitchApplicationPermissions();
				return result;
			}
			catch
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, "No permissions found");
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// GetUserPermissions - Get a list of permissions associated with a user
		// Verb: Get
		// Data: id (UserName)
		// Returns: Json list of permissions associated to this user
		// =======================================================================================================
		[HttpGet]
		public IEnumerable<PermissionDTO> GetUserPermissions(string id)
		{
			try
			{
				// Call our internal helper function that gets the LS permissions
				var result = GetLightSwitchUserPermissions(id);
				return result;
			}
			catch
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, "No permissions assigned to user");
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// GetRolePermissions - Get a list of permissions associated with a role
		// Verb: Get
		// Data: id (RoleName)
		// Returns:  Json list of permissions associated to this role
		// =======================================================================================================
		[HttpGet]
		public IEnumerable<RolePermissionDTO> GetRolePermissions(string id)
		{
			try
			{
				// Ok going to stop the comments now as its pretty obvious whats going on
				var result = GetLightSwitchRolePermissions(id);
				return result;
			}
			catch
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, "No permissions assigned to role");
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// GetRoleUsers - Get a list of users associated with a role
		// Verb: Get
		// Data: id (RoleName)
		// Returns:  Json list of users associated to this role
		// =======================================================================================================
		[HttpGet]
		public IEnumerable<RoleUserDTO> GetUsersInRole(string id)
		{
			try
			{
				var result = (from r in Roles.GetUsersInRole(id) 
							  select new RoleUserDTO { RoleName = id, UserName = r });

				return result;
			}
			catch
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.NotFound, "No Users assigned to role");
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// CreateUser - Create a new user
		// Verb: Post
		// Data: UserRegistrationDTO in the body of the request
		// Returns:  Json representation of the created user
		// =======================================================================================================
		[HttpPost]
		public UserDTO CreateUser([FromBody] UserDTO jsonData)
		{
			try
			{
				var result = new UserDTO();

				// If no UserName then throw an exception so our catch can handle it
				if (jsonData.UserName == "") throw new Exception("No UserName");

				// If no Password then throw an exception so our catch can handle it
				if (jsonData.Password == "") throw new Exception("No Password");

				// Make sure we have a ServerApplicationContext, if unable to, throw an exception
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null)
					throw new Exception("Could not create a ServerApplicationContext... SAC4");

				using (ServerApplicationContext.Current)
				{
					// Check the password for validity
					if(jsonData.Password.ToLower().Contains(jsonData.UserName.ToLower()) ||
						!ServerApplicationContext.Current.DataWorkspace.SecurityData.IsValidPassword(jsonData.Password)) 
						throw new Exception("Not a valid password");
					
					// First order of business is to create our LightSwitch user
					var newUser = ServerApplicationContext.Current.DataWorkspace.SecurityData.UserRegistrations.AddNew();
					newUser.FullName = jsonData.FullName;
					newUser.UserName = jsonData.UserName.ToLower();
					newUser.Password = jsonData.Password;

					ServerApplicationContext.Current.DataWorkspace.SecurityData.SaveChanges();
				}

				// Successful creation of the user if we got this far
				// Go get our asp user to stuff data
				var user = Membership.GetUser(jsonData.UserName);
				if (user != null)
				{
					user.Email = jsonData.Email;
					Membership.UpdateUser(user);

					// So we're good to go... stuff our return DTO for validation on client side
					result.UserName = user.UserName;
					result.FullName = jsonData.FullName;
					result.Email = user.Email;
					result.IsLockedOut = user.IsLockedOut;
				}

				// Do not send back the password, so blank it out
				result.Password = "";
				return result;
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// CreateRole - Create a new role
		// Verb: Get
		// Data: id (RoleName)
		// Returns: Nothing
		// =======================================================================================================
		[HttpPost]
		public RoleDTO CreateRole([FromBody] RoleDTO jsonData)
		{
			try
			{
				Roles.CreateRole(jsonData.RoleName);
				return jsonData;
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// UpdateUser - Updates an existing user profile.  At this time only FullName gets updated
		// Verb: Put
		// Data: UserProfileDTO in the body of the request
		// Returns: Nothing
		// Comments:  This can be easily expanded, allowing for a much richer user profile table vs LightSwitch
		// =======================================================================================================
		[HttpPut]
		public UserDTO UpdateUser([FromBody] UserDTO jsonData)
		{
			try
			{
				var result = new UserDTO();

				// Get our user
				var user = Membership.GetUser(jsonData.UserName);

				// If user was not found, throw an exception so the client can be notified
				if (user == null) throw new Exception("User was not found");

				// First lets check if the account needs to be unlocked
				if (user.IsLockedOut && !jsonData.IsLockedOut)
					user.UnlockUser();

				user.Email = jsonData.Email.ToLower();

				// Update ASP.NET Membership
				Membership.UpdateUser(user);

				// If we have a fullname, go update the LightSwitch profile
				if (jsonData.FullName != "")
				{
					// Make sure we have a ServerApplicationContext, if unable to, throw an exception
					if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
					if (ServerApplicationContext.Current == null)
						throw new Exception("Could not create a ServerApplicationContext... SAC5");

					using (ServerApplicationContext.Current)
					{
						// Find our LightSwitch user
						var lsUser = ServerApplicationContext.Current.DataWorkspace.SecurityData.UserRegistrations_Single(jsonData.UserName);

						// Was a password sent across?
						if (!string.IsNullOrEmpty(jsonData.Password))
						{
							// Check the password for validity
							if (jsonData.Password.ToLower().Contains(jsonData.UserName.ToLower()) ||
							    !ServerApplicationContext.Current.DataWorkspace.SecurityData.IsValidPassword(jsonData.Password))
								throw new Exception("Not a valid password");

							// Change the password
							lsUser.Password = jsonData.Password;
						}

						// Update the profile property and save
						lsUser.FullName = jsonData.FullName;

						// Save the data
						ServerApplicationContext.Current.DataWorkspace.SecurityData.SaveChanges();
					}
				}

				// If we got here, all was successful, so stuff our return DTO for client validation
				result.UserName = user.UserName;
				result.FullName = jsonData.FullName;
				result.Email = user.Email;
				result.Password = "";
				result.IsLockedOut = user.IsLockedOut;
				result.IsOnline = user.IsOnline;

				return result;
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// AddPermissionToRole - Add a LightSwitch permission to an existing role
		// Verb: Post
		// Data: RolePermissionDTO in the body of the request
		// Returns: The role permission for client validation
		// Comment: Permissions are created/located in the Web.config file
		// =======================================================================================================
		[HttpPost]
		public RolePermissionDTO AddPermissionToRole([FromBody] RolePermissionDTO jsonData)
		{
			try
			{
				// If the Add failed, throw an exception to the client
				if (!AddLightSwitchPermissionToRole(jsonData.PermissionId, jsonData.RoleName)) throw new Exception("Could not add permission to role");

				// Else, return the DTO for client validation
				return GetLightSwitchRolePermissions(jsonData.RoleName).FirstOrDefault(p => p.PermissionId == jsonData.PermissionId);
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}

		}


		// =======================================================================================================
		// RemovePermissionFromRole - Remove a LightSwitch permission from a role
		// Verb: Delete
		// Data: RolePermissionDTO in the body of the request
		// Returns: Nothing
		// =======================================================================================================
		[HttpPost]
		public void RemovePermissionFromRole([FromBody] RolePermissionDTO jsonData)
		{
			try
			{
				RemoveLightSwitchPermissionFromRole(jsonData.RoleName, jsonData.PermissionId);
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// AddUserToRole - Add an existing user to a role
		// Verb: Post
		// Data: RoleUserDTO in the body of the request
		// Returns: Nothing
		// =======================================================================================================
		[HttpPost]
		public UserRoleDTO AddUserToRole([FromBody] UserRoleDTO jsonData)
		{
			try
			{
				Roles.AddUserToRole(jsonData.UserName, jsonData.RoleName);
				return jsonData;
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// RemoveUserFromRole - Remove a user from an existing role
		// Verb: Delete
		// Data: RoleUserDTO in the body of the request
		// Returns: Nothing
		// =======================================================================================================
		[HttpPost]
		public void RemoveUserFromRole([FromBody] UserRoleDTO jsonData)
		{
			try
			{
				Roles.RemoveUserFromRole(jsonData.UserName, jsonData.RoleName);
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// DeleteRole - Delete a role
		// Verb: Delete
		// Data: Id (RoleName)
		// Returns: Nothing
		// =======================================================================================================
		[HttpPost]
		public void DeleteRole([FromBody] RoleDTO jsonData)
		{
			try
			{
				// Remove the role from all users
				var allRoleUsers = Roles.GetUsersInRole(jsonData.RoleName);

				foreach (var user in allRoleUsers)
				{
					Roles.RemoveUserFromRole(user, jsonData.RoleName);
				}

				// Remove all the permission associations
				var lsRolePermissions = GetLightSwitchRolePermissions(jsonData.RoleName);

				foreach (var permission in lsRolePermissions)
				{
					RemoveLightSwitchPermissionFromRole(permission.RoleName, permission.PermissionId);
				}

				// Now go delete the role
				Roles.DeleteRole(jsonData.RoleName);
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// DeleteUser - Delete a registered user
		// Verb: Delete
		// Data: Id (UserName)
		// Returns: Nothing
		// =======================================================================================================
		[HttpPost]
		public void DeleteUser([FromBody] UserDTO jsonData)
		{
			try
			{
				// Make sure we have a ServerApplicationContext, if unable to, throw an exception
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null)
					throw new Exception("Could not create a ServerApplicationContext... SAC6");

				using (ServerApplicationContext.Current)
				{
					var dbContext = ServerApplicationContext.Current.DataWorkspace;

					// Remove the user from all associated roles
					var userRoles = Roles.GetRolesForUser(jsonData.UserName);
					if (userRoles.Any())
						Roles.RemoveUserFromRoles(jsonData.UserName, userRoles);

					// Now delete the User, LightSwitch will also take care of the ASP.NET tables
					var lsUser = dbContext.SecurityData.UserRegistrations_SingleOrDefault(jsonData.UserName);

					// Could not find the user... so return... no reason to let the client know
					if (lsUser == null) return;

					lsUser.Delete();
					dbContext.SecurityData.SaveChanges();
				}
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}

		}


		// =======================================================================================================
		// ChangePassword - Change the password of an existing user
		// Verb: Put
		// Data: ChangePasswordDTO in the body of the request
		// Returns: The user, for client validation of success
		// =======================================================================================================
		[HttpPut]
		public MembershipUser ChangePassword([FromBody] ChangePasswordDTO jsonData)
		{
			try
			{
				// Go get our user
				var user = Membership.GetUser(jsonData.UserName);

				// If we found, attempt to change the password, if it fails send an error back
				if (user != null && !user.ChangePassword(jsonData.OldPassword, jsonData.NewPassword))
					throw new Exception("Password change failed");

				return user;
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}

		}


		// =======================================================================================================
		// Login - Log a user in, return authentication cookie
		// Verb: Post
		// Data: LoginDTO in the body of the request
		// Returns: Json representation of the successful logged in user
		// Comment:  Normally you would use the aspx page for logging in, but this is good for SPAs
		// =======================================================================================================
		[AllowAnonymous]
		[HttpPost]
		public User Login([FromBody] LoginDTO jsonData)
		{
			try
			{
				// Create a new instance of the LightSwitch Authentication Service
				var authService = new AuthenticationService();

				// Log our user in
				var user = authService.Login(jsonData.UserName, jsonData.Password, jsonData.Persistent, null);

				// Successful login?  If so, return the user
				if (user != null) return user;

				// If failed... set status for return
				var response = Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Login failed.  Check User Name and/or Password.");
				throw new HttpResponseException(response);

			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}

		}


		// =======================================================================================================
		// Logout - Clears the cookie, effectively logging a user out of the system
		// Verb: Get
		// Data: None
		// Returns: Json representation of the successful logged in user
		// Comment:  Normally you would use the aspx page for logging out, but this is good for SPAs
		// =======================================================================================================
		[AllowAnonymous]
		[HttpGet]
		public User Logout()
		{
			try
			{
				var authService = new AuthenticationService();
				var user = authService.Logout();

				// Successful logout, return the user for client validation
				if (user != null) return user;

				// Else we had an error, throw the exceptions
				throw new Exception("Odd... Logout failed!!");
			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}

		}


		// =======================================================================================================
		// IsAuthenticated - Simple... if they were able to get into the function, they are authenticated
		// Verb: Get
		// Data: None
		// Returns: bool
		// =======================================================================================================
		[HttpGet]
		public bool IsAuthenticated()
		{
			return true;
		}


		// =======================================================================================================
		// LoggedInUser - Get the user data of the currently logged in user
		// Verb: Get
		// Data: None
		// Returns: Json representation of the current logged in user
		// =======================================================================================================
		[HttpGet]
		public object LoggedInUser()
		{
			try
			{
				// Can add what you want to this object depending on requirements
				var result = new { User.Identity.Name, User.Identity.IsAuthenticated };

				return result;

			}
			catch (Exception e)
			{
				var response = Request.CreateErrorResponse(HttpStatusCode.Conflict, e.Message);
				throw new HttpResponseException(response);
			}
		}


		// =======================================================================================================
		// UnlockUser - Unlock a locked account, typically from entering a password wrong too many times.
		// Verb: Get
		// Data: None
		// Returns: bool based on success
		// =======================================================================================================
		[HttpGet]
		public bool UnlockUser(string id)
		{
			// Go get our user
			var user = Membership.GetUser(id);

			// Simple... unlock the account
			return user != null && user.UnlockUser();
		}


		// =====================================================================================
		// =====================================================================================
		// Helper functions 
		// Key item to note... there can only be 1 instance of the ServerApplicationContext!
		// =====================================================================================
		// =====================================================================================

		#region helpers


		// =======================================================================================================
		// Helper - Get the permissions for this LightSwitch application
		// Data: None
		// Returns: List of PermissionDTOs
		// =======================================================================================================
		private static IEnumerable<PermissionDTO> GetLightSwitchApplicationPermissions()
		{
			try
			{
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null) 
					throw new Exception("Could not create a ServerApplicationContext... SAC7");

				using (ServerApplicationContext.Current)
				{
					var dbContext = ServerApplicationContext.Current.DataWorkspace;

					var resultList = (from Permission p in dbContext.SecurityData.Permissions 
									  select new PermissionDTO {Id = p.Id, Name = p.Name}).ToList();

					return resultList;
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Message, e.InnerException);
			}
		}


		// =======================================================================================================
		// Helper - Get the permissions for an individual user
		// Data: UserName
		// Returns: List of PermissionDTOs
		// Comment: This could probably be done a bit better with some extra Linq
		// =======================================================================================================
		private static IEnumerable<PermissionDTO> GetLightSwitchUserPermissions(string userName)
		{
			try
			{
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null) 
					throw new Exception("Could not create a ServerApplicationContext... SAC8");

				using (ServerApplicationContext.Current)
				{
					var dbContext = ServerApplicationContext.Current.DataWorkspace;

					// List to hold our permissions
					var permissionList = new List<PermissionDTO>();

					// All the roles assigned to the user
					var assignedRoles = dbContext.SecurityData.RoleAssignments
						.Where(ra => ra.UserName == userName)
						.Select(r => r.Role)
						.Execute();

					// Get all the Role Assignments
					var roleAssignments = assignedRoles.Select(r => r.RolePermissions);

					// Loop over our Assignments 
					foreach (var r in roleAssignments)
					{
						permissionList.AddRange(r.Select(p => new PermissionDTO {Id = p.Permission.Id, Name = p.Permission.Name}));
					}

					return permissionList;
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Message, e.InnerException);
			}
		}


		// =======================================================================================================
		// Helper - Get all the permissions for a particular role
		// Data: RoleName
		// Returns: List of Permisisons for the role, RolePermissionDTO
		// =======================================================================================================
		private static IEnumerable<RolePermissionDTO> GetLightSwitchRolePermissions(string roleName)
		{
			try
			{
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null) 
					throw new Exception("Could not create a ServerApplicationContext... SAC9");

				using (ServerApplicationContext.Current)
				{
					var dbContext = ServerApplicationContext.Current.DataWorkspace;

					var result = dbContext.SecurityData.RolePermissions
						.Where(r => r.RoleName == roleName)
						.Select(rp => new RolePermissionDTO {RoleName = rp.RoleName, PermissionId = rp.PermissionId})
						.Execute();

		
					return result;
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Message, e.InnerException);
			}
		}


		// =======================================================================================================
		// Helper - Add a permission to a role
		// Data: LightSwitch PermissionId and RoleName
		// Returns: bool 
		// =======================================================================================================
		private static bool AddLightSwitchPermissionToRole(string permissionId, string roleName)
		{
			try
			{
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null)
					throw new Exception("Could not create a ServerApplicationContext... SAC10");

				using (ServerApplicationContext.Current)
				{

					var dbContext = ServerApplicationContext.Current.DataWorkspace;

					// Find our role... exception if not found
					var role = dbContext.SecurityData.Roles_Single(roleName);

					// Find our permission... exception if not found
					var permission = dbContext.SecurityData.Permissions_Single(permissionId);

					// Does the association already exist, if so, return
					if (dbContext.SecurityData.RolePermissions_SingleOrDefault(roleName, permissionId) != null) return true;

					// It does not... add a new association entity
					var newPermissionAssoc = dbContext.SecurityData.RolePermissions.AddNew();

					// Assign the role 
					newPermissionAssoc.Role = role;

					// Assign the permission and save
					newPermissionAssoc.Permission = permission;
					dbContext.SecurityData.SaveChanges();

					// We got this far, so success...return true
					return true;
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Message, e.InnerException);
			}
		}


		// =======================================================================================================
		// Helper - Remove Permission from a role
		// Data: RoleName, LightSwitch PermissionId 
		// Returns: Nothing
		// =======================================================================================================
		private static void RemoveLightSwitchPermissionFromRole(string roleName, string permissionId)
		{
			try
			{
				if (ServerApplicationContext.Current == null) ServerApplicationContext.CreateContext();
				if (ServerApplicationContext.Current == null)
					throw new Exception("Could not create a ServerApplicationContext... SAC11");

				using (ServerApplicationContext.Current)
				{

					var dbContext = ServerApplicationContext.Current.DataWorkspace;

					// Find our role/permission association entity
					var rolePermission = dbContext.SecurityData.RolePermissions_Single(roleName, permissionId);

					// Delete the entity and save
					rolePermission.Delete();
					dbContext.SecurityData.SaveChanges();
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Message, e.InnerException);
			}
		}


		#endregion helpers

	}

}