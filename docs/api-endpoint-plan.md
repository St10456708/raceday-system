# RaceDay API Endpoint Plan

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None (public) | { fullName, email, password, role, phoneNumber } | 201 Created - returns new user id and auth token. 400 Bad Request - missing/invalid fields. 409 Conflict - email already registered. |
| POST | /api/auth/login | Authenticates a user and returns a JWT for subsequent requests. | None (public) | { email, password } | 200 OK - returns token and role. 401 Unauthorized - invalid email or password. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any (logged in) | None | 200 OK - returns user profile. 401 Unauthorized - no/invalid token. |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any (logged in) | { fullName, phoneNumber } | 200 OK - returns updated profile. 400 Bad Request - invalid data. 401 Unauthorized. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events | Creates a new event owned by the logged-in organiser. | Organiser | { eventName, description, eventDate, eventType, venue: { venueName, address, city } } | 201 Created - returns new event with id. 400 Bad Request - missing fields. 401/403 - not an organiser. |
| GET | /api/events | Lists all upcoming events. Publicly browsable. | None (public) | None | 200 OK - returns array of events. |
| GET | /api/events/{id} | Returns full details of one event, including venue and categories. | None (public) | None | 200 OK - returns event details. 404 Not Found - event does not exist. |
| PUT | /api/events/{id} | Updates an event. Only the organiser who created it may edit it. | Organiser (owner) | { eventName, description, eventDate, eventType, status } | 200 OK - returns updated event. 403 Forbidden - not the owning organiser. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in organiser. | Organiser (owner) | None | 204 No Content - deleted successfully. 403 Forbidden - not the owner. 404 Not Found. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{id}/categories | Adds a race category (e.g. 10km Run) to an event. | Organiser (owner) | { categoryName, distanceKm, price, maxParticipants } | 201 Created - returns new category. 403 Forbidden - not owner. 404 Not Found - event does not exist. |
| GET | /api/events/{id}/categories | Lists all categories available for an event. | None (public) | None | 200 OK - returns array of categories. 404 Not Found - event does not exist. |
| PUT | /api/categories/{id} | Updates a category's details. | Organiser (owner) | { categoryName, distanceKm, price, maxParticipants } | 200 OK - returns updated category. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (owner) | None | 204 No Content. 403 Forbidden. 404 Not Found. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{id}/enrol | Enrols the logged-in participant into a race category and assigns a bib number. | Participant | None (participant identified from token) | 201 Created - returns enrolment with bib number. 400 Bad Request - category is full. 401 Unauthorized. 409 Conflict - already enrolled in this category. |
| GET | /api/users/me/enrolments | Lists the logged-in participant's current and past enrolments. | Participant | None | 200 OK - returns array of enrolments. 401 Unauthorized. |
| DELETE | /api/enrolments/{id} | Cancels an enrolment belonging to the logged-in participant. | Participant (owner) | None | 204 No Content. 403 Forbidden - not the owner. 404 Not Found. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{id}/result | Captures a finishing result for a participant's enrolment. | Organiser (of the event) | { finishTime, position, status } | 201 Created - returns new result. 403 Forbidden - not the event organiser. 404 Not Found - enrolment does not exist. |
| GET | /api/events/{id}/results | Lists all results for an event (leaderboard). | None (public) | None | 200 OK - returns array of results. 404 Not Found - event does not exist. |
| GET | /api/users/me/results | Returns the logged-in participant's personal performance history across all events. | Participant | None | 200 OK - returns array of past results. 401 Unauthorized. |
Improve formatting of API endpoint plan