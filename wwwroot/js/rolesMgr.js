/**
 * Removed specified element from selected parental element
 * @param {string} role
 */
function specialRemLi(role)
{
    // Remove user roles list entry
    let parent = document.getElementById("allRoles");
    let elem = document.getElementById(role);
    if (elem) // double-check if exists
    {
        parent.removeChild(elem);
    }

    // Insert entry in drop-down role list
    let newParent = document.getElementById("dropdownList");
    let newElem = document.createElement("a");

    newElem.Id = role;
    newElem.onclick = (event) => {
        specialAddLi(`${role}`);
    };
    newElem.innerHTML = role;

    newParent.appendChild(newElem);
}

/**
 * Creates special <li> entry with value == Id on lastElement-1 index
 * @param {string} role
 */
function specialAddLi(role)
{
    // Remove entry in drop-down list
    let oldParent = document.getElementById("dropdownList");
    let oldElem = document.getElementById(role);
    oldParent.removeChild(oldElem);

    // Create new entry in user roles list
    let parent = document.getElementById("allRoles");
    let elem = document.createElement("li");

    let innerHtml =
        `<button type="button" class="btn-small-danger" onclick="specialRemLi('${role}');">
        <svg role="img" height="16" viewBox="0 0 16 16" width="16">
        <path d="M3.72 3.72a.75.75 0 0 1 1.06 0L8 6.94l3.22-3.22a.749.749 0 0 1 1.275.326.749.749 0 0 1-.215.734L9.06 8l3.22 3.22a.749.749 0 0 1-.326 1.275.749.749 0 0 1-.734-.215L8 9.06l-3.22 3.22a.751.751 0 0 1-1.042-.018.751.751 0 0 1-.018-1.042L6.94 8 3.72 4.78a.75.75 0 0 1 0-1.06Z"></path>
        </svg>${role}</button>`;

    elem.id = role;
    elem.innerHTML = innerHtml;

    parent.insertBefore(elem, parent.lastElementChild);
}
